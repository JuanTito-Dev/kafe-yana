using KafeYana.Infrastructure.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// BackgroundService que sincroniza el catálogo paramétrico de métodos de
    /// pago del SIAT en la BD local <c>CatMetodosPago</c>.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor: sincronización inmediata (para que el
    ///      caché estático <c>MetodoPagoSiatCatalogo</c> tenga descripciones
    ///      oficiales del SIN antes de la primera venta facturada).
    ///
    /// **NO hay loop diario.** Decisión confirmada (jun-2026): el catálogo
    /// tiene ~308 entradas y cambia muy poco, no se justifica un sync
    /// diario. El operador puede forzar la recarga con
    /// <c>POST /api/catalogos/sincronizar-metodos-pago</c>.
    ///
    /// Espejo de <see cref="SincronizacionTipoEmisionHostedService"/> pero
    /// sin el while del loop diario.
    /// </summary>
    public class SincronizacionMetodosPagoHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionMetodosPagoHostedService> _logger;

        public SincronizacionMetodosPagoHostedService(
            IServiceProvider services,
            ILogger<SincronizacionMetodosPagoHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SincronizacionMetodosPagoHostedService iniciando (boot + manual; sin loop diario)");

            try
            {
                await IntentarSincronizarAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown normal.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en la sincronización de boot de CatMetodosPago. "
                    + "El operador puede forzar el sync vía POST /api/catalogos/sincronizar-metodos-pago.");
            }

            // Mantener el BackgroundService vivo (sin loop) hasta el shutdown.
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "SincronizacionMetodosPagoHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var sincronizador = scope.ServiceProvider
                .GetRequiredService<SincronizadorCatMetodosPago>();
            var (total, nuevos, actualizados, pvs) =
                await sincronizador.SincronizarAsync(ct);
            _logger.LogInformation(
                "Sincronización CatMetodosPago OK ({Total} totales del SIN, {Nuevos} nuevos, {Actualizados} actualizados, {PVs} PVs)",
                total, nuevos, actualizados, pvs);
        }
    }
}