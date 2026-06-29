using KafeYana.Infrastructure.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// BackgroundService que mantiene actualizado el catálogo paramétrico de
    /// unidades de medida del SIAT en la BD local <c>CatUnidadesMedida</c>.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor: sincronización inmediata (para que el
    ///      caché estático <c>UnidadMedidaSiatCatalogo</c> tenga descripciones
    ///      oficiales del SIN antes de la primera venta facturada).
    ///   2) Luego, todos los días a las <b>08:10 BOT (UTC-4, sin DST)</b>.
    ///
    /// El cálculo del delay lo hace <see cref="BoliviaScheduleHelper.UntilNextRun"/>,
    /// que compensa si el server arrancó tarde o estuvo caído varios días
    /// (no se acumulan ticks perdidos).
    ///
    /// Espejo de <see cref="SincronizacionTipoEmisionHostedService"/>,
    /// <see cref="SincronizacionTipoDocumentoIdentidadHostedService"/> y
    /// <see cref="SincronizacionPaisOrigenHostedService"/>.
    /// </summary>
    public class SincronizacionUnidadMedidaHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionUnidadMedidaHostedService> _logger;

        /// <summary>Hora objetivo diaria en hora BOT.</summary>
        private static readonly TimeSpan HoraObjetivo = new(8, 10, 0);

        public SincronizacionUnidadMedidaHostedService(
            IServiceProvider services,
            ILogger<SincronizacionUnidadMedidaHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SincronizacionUnidadMedidaHostedService iniciando (diario a las {Hora} {Tz})",
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
                        "Próxima sincronización de unidades de medida en {Delay}",
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    await IntentarSincronizarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "SincronizacionUnidadMedidaHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            try
            {
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatUnidadMedida>();
                var (total, nuevos, actualizados, pvs) =
                    await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatUnidadesMedida OK ({Total} totales del SIN, {Nuevos} nuevos, {Actualizados} actualizados, {PVs} PVs)",
                    total, nuevos, actualizados, pvs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatUnidadesMedida. "
                    + "Se reintentará en el siguiente tick.");
            }
        }
    }
}