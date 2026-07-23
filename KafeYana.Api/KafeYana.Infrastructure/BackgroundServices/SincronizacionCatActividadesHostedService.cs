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
    /// BackgroundService que mantiene actualizados los catálogos de actividades
    /// económicas, documentos sectoriales y la matriz actividad↔sector del SIAT
    /// en la BD local.
    ///
    /// Calendario:
    ///   1) Al arrancar el servidor (BD vacía → no se puede facturar hasta que termine).
    ///   2) Todos los días a las <b>08:10 BOT (UTC-4, sin DST)</b>.
    ///
    /// Migrado de <c>PeriodicTimer(24h)</c> a schedule explícito para que la
    /// hora del sync NO dependa del momento exacto en que arrancó el server
    /// (antes, si el server arrancaba a las 8:05, el sync corría a las 8:05;
    /// ahora SIEMPRE espera hasta las 8:10 BOT). Si el server estuvo caído
    /// varios días, al levantar calcula la próxima 8:10 BOT futura y dispara
    /// una vez (no acumula ticks perdidos).
    ///
    /// El cálculo lo hace <see cref="BoliviaScheduleHelper"/> (compartido con
    /// <see cref="SincronizacionMotivoAnulacionHostedService"/> y
    /// <see cref="SincronizacionLeyendaHostedService"/>).
    /// </summary>
    public class SincronizacionCatActividadesHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SincronizacionCatActividadesHostedService> _logger;

        /// <summary>Hora objetivo diaria en hora BOT.</summary>
        private static readonly TimeSpan HoraObjetivo = new(8, 10, 0);

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
                "SincronizacionCatActividadesHostedService iniciando (diario a las {Hora} {Tz})",
                HoraObjetivo, BoliviaScheduleHelper.BoliviaTz.DisplayName);

            // 1) Sincronización inicial al arrancar
            await IntentarSincronizarAsync(stoppingToken);

            // 2) Loop diario
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = BoliviaScheduleHelper.UntilNextRun(HoraObjetivo);
                    _logger.LogInformation(
                        "Próxima sincronización de actividades y derivados en {Delay}",
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested) break;

                    await IntentarSincronizarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "SincronizacionCatActividadesHostedService detenido por shutdown");
            }
        }

        private async Task IntentarSincronizarAsync(CancellationToken ct)
        {
            // Cada sincronizador es Scoped (depende de ICuisService que también es Scoped),
            // por eso creamos un scope explícito desde el HostedService (Singleton).
            // Si uno falla, seguimos con el otro: un fallo no debe bloquear la sync del otro catálogo.
            using var scope = _services.CreateScope();

            try
            {
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

            try
            {
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatDocumentoSector>();
                var cantidad = await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatDocumentosSector OK ({Cantidad} filas)",
                    cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatDocumentosSector. "
                    + "Se reintentará en el siguiente tick.");
            }

            try
            {
                var sincronizador = scope.ServiceProvider
                    .GetRequiredService<SincronizadorCatActividadDocumentoSector>();
                var cantidad = await sincronizador.SincronizarAsync(ct);
                _logger.LogInformation(
                    "Sincronización CatActividadesDocumentosSector OK ({Cantidad} filas)",
                    cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en sincronización periódica de CatActividadesDocumentosSector. "
                    + "Se reintentará en el siguiente tick.");
            }
        }
    }
}
