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
    /// eventos significativos del SIAT en la tabla local <c>CatEventosSignificativos</c>.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor: sincronización inmediata (para tener el
    ///      catálogo SIN cargado antes de que se necesite en el flujo de
    ///      contingencia, que se implementará en un ticket separado).
    ///   2) Luego, todos los días a las <b>08:10 BOT (UTC-4, sin DST)</b>.
    ///
    /// El cálculo del delay lo hace <see cref="BoliviaScheduleHelper.UntilNextRun"/>,
    /// que compensa si el server arrancó tarde o estuvo caído varios días
    /// (no se acumulan ticks perdidos).
    ///
    /// Espejo de <see cref="SincronizacionCodigosSiatHostedService"/> y
    /// <see cref="SincronizacionLeyendaHostedService"/>.
    /// </summary>
    public class SincronizacionEventoSignificativoHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionEventoSignificativoHostedService> _logger;

        /// <summary>Hora objetivo diaria en hora BOT.</summary>
        private static readonly TimeSpan HoraObjetivo = new(8, 10, 0);

        public SincronizacionEventoSignificativoHostedService(
            IServiceProvider services,
            ILogger<SincronizacionEventoSignificativoHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SincronizacionEventoSignificativoHostedService iniciando (diario a las {Hora} {Tz})",
                HoraObjetivo, BoliviaScheduleHelper.BoliviaTz.DisplayName);

            // 1) Sincronización inmediata al boot para que el catálogo esté
            //    disponible desde el primer pedido. Si falla, seguimos
            //    esperando al próximo tick.
            await IntentarSincronizarAsync(stoppingToken);

            // 2) Loop diario
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = BoliviaScheduleHelper.UntilNextRun(HoraObjetivo);
                    _logger.LogInformation(
                        "Próxima sincronización de eventos significativos en {Delay}",
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    await IntentarSincronizarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "SincronizacionEventoSignificativoHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            try
            {
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatEventoSignificativo>();
                var (cantidad, pvs) = await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatEventosSignificativos OK ({Cantidad} filas, {PVs} PVs)",
                    cantidad, pvs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatEventosSignificativos. "
                    + "Se reintentará en el siguiente tick.");
            }
        }
    }
}
