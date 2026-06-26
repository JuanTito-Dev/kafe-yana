using KafeYana.Infrastructure.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// BackgroundService que mantiene actualizado el catálogo paramétrico de
    /// motivos de anulación del SIAT en la BD local.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor: sincronización inmediata (para que la BD
    ///      tenga descripciones oficiales del SIN antes de la primera factura
    ///      o nota que se intente anular).
    ///   2) Luego, todos los días a las <b>08:10 BOT (UTC-4, sin DST)</b>.
    ///
    /// El cálculo del delay y el TZ de Bolivia los hace
    /// <see cref="BoliviaScheduleHelper"/> (compartido con los otros hosted
    /// services de sync — Actividades migrado, Leyenda nuevo).
    /// </summary>
    public class SincronizacionMotivoAnulacionHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionMotivoAnulacionHostedService> _logger;

        /// <summary>Hora objetivo diaria en hora BOT.</summary>
        private static readonly TimeSpan HoraObjetivo = new(8, 10, 0);

        public SincronizacionMotivoAnulacionHostedService(
            IServiceProvider services,
            ILogger<SincronizacionMotivoAnulacionHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SincronizacionMotivoAnulacionHostedService iniciando (diario a las {Hora} {Tz})",
                HoraObjetivo, BoliviaScheduleHelper.BoliviaTz.DisplayName);

            // 1) Sincronización inmediata al boot para tener el catálogo antes de
            //    cualquier anulación. Si falla, seguimos esperando al próximo tick.
            await IntentarSincronizarAsync(stoppingToken);

            // 2) Loop diario. Se computa el tiempo hasta la próxima 08:10 BOT en
            //    cada iteración para que, si el server estuvo caído varias horas,
            //    reanude al siguiente slot sin acumular ticks perdidos.
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = BoliviaScheduleHelper.UntilNextRun(HoraObjetivo);
                    _logger.LogInformation(
                        "Próxima sincronización de motivos de anulación en {Delay}",
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    await IntentarSincronizarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "SincronizacionMotivoAnulacionHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            try
            {
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatMotivoAnulacion>();
                var (cantidad, pvs) = await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatMotivosAnulacion OK ({Cantidad} filas, {PVs} PVs)",
                    cantidad, pvs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatMotivosAnulacion. "
                    + "Se reintentará en el siguiente tick.");
            }
        }
    }
}
