using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// IHostedService de bootstrap que hidrata el
    /// <see cref="ISiatConnectivityMonitor"/> al arrancar el backend.
    ///
    /// Si encuentra contingencias activas en BD (caso: server crasheó durante
    /// contingencia y se reinicia), encola cada eventoId en el
    /// <see cref="ContingencyResendHostedService"/> para que las ventas
    /// pendientes se reenvíen automáticamente sin esperar al próximo cobro.
    ///
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class ContingencyBootstrapHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ContingencyBootstrapHostedService> _logger;

        public ContingencyBootstrapHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<ContingencyBootstrapHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var monitor = scope.ServiceProvider
                    .GetRequiredService<ISiatConnectivityMonitor>();

                // Hidratar el monitor desde BD. Importante: este método NO
                // asume que el SIAT está caído. Solo lee la BD.
                await monitor.InicializarAsync(cancellationToken);

                var eventoService = scope.ServiceProvider
                    .GetRequiredService<IEventoSignificativoSiatService>();

                var contingencias = await eventoService.ListarContingenciasActivasAsync(cancellationToken);

                if (contingencias.Count == 0)
                {
                    _logger.LogInformation(
                        "ContingencyBootstrapHostedService: no hay contingencias activas en BD.");
                    return;
                }

                _logger.LogInformation(
                    "ContingencyBootstrapHostedService: {N} contingencia(s) activa(s) en BD. "
                  + "Reanudando reenvíos de ventas pendientes.",
                    contingencias.Count);

                // Publicamos cada evento a través del monitor para que el
                // ContingencyResendHostedService los consuma por la misma
                // vía que una recuperación en runtime.
                foreach (var c in contingencias)
                {
                    monitor.PublicarRecuperacion(
                        c.CodigoSucursal, c.CodigoPuntoVenta, c.EventoSignificativoId);

                    _logger.LogInformation(
                        "Reanudando contingencia activa: suc={Suc}, pv={PV}, eventoId={Id}, origen={Origen}",
                        c.CodigoSucursal, c.CodigoPuntoVenta,
                        c.EventoSignificativoId, c.Origen);
                }
            }
            catch (Exception ex)
            {
                // No tirar el arranque si falla la hidratación: el monitor
                // operará "frío" y reconstruirá el estado desde los cobros.
                _logger.LogError(ex,
                    "ContingencyBootstrapHostedService: error hidratando monitor. "
                  + "El sistema seguirá operando; las contingencias activas se detectarán al próximo cobro.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}