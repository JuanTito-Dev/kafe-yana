using KafeYana.Infrastructure.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// BackgroundService que mantiene actualizado el catálogo de actividades
    /// económicas del SIAT en la BD local.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor (BD vacía → no se puede facturar hasta que termine).
    ///   2) Cada 24 horas (PeriodicTimer).
    ///
    /// Si la sincronización falla, se loguea el error y se reintenta en el siguiente tick
    /// (no se cae el proceso).
    /// </summary>
    public class SincronizacionCatActividadesHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionCatActividadesHostedService> _logger;

        // Producción: cada 24h. En Testing se puede bajar.
        private static readonly TimeSpan Periodo = TimeSpan.FromHours(24);

        public SincronizacionCatActividadesHostedService(
            IServiceProvider services,
            ILogger<SincronizacionCatActividadesHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SincronizacionCatActividadesHostedService iniciando (periodo: {Periodo})",
                Periodo);

            // 1) Sincronización inicial al arrancar
            await IntentarSincronizarAsync(stoppingToken);

            // 2) Loop cada 24h
            using var timer = new PeriodicTimer(Periodo);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await IntentarSincronizarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del servidor
                _logger.LogInformation(
                    "SincronizacionCatActividadesHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            try
            {
                // SincronizadorCatActividades es Scoped (depende de ICuisService que también es Scoped),
                // por eso creamos un scope explícito desde el HostedService (Singleton).
                using var scope = _services.CreateScope();
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatActividades>();

                var cantidad = await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatActividades OK ({Cantidad} filas)",
                    cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatActividades. "
                    + "Se reintentará en el siguiente tick.");
            }
        }
    }
}