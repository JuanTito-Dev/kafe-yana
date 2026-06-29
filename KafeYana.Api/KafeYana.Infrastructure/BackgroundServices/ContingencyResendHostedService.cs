using System.Threading.Channels;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// BackgroundService que consume reenvíos de facturas contingencia.
    /// Se suscribe a <see cref="ISiatConnectivityMonitor.OnRecuperacionDetectada"/>
    /// y encola el eventoId en un <see cref="Channel{T}"/> interno. Un único
    /// loop procesa los eventos secuencialmente para no saturar el SIAT con
    /// ráfagas de reenvíos.
    ///
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class ContingencyResendHostedService : BackgroundService
    {
        private readonly ISiatConnectivityMonitor _monitor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ContingencyResendHostedService> _logger;
        private readonly Channel<int> _queue;

        public ContingencyResendHostedService(
            ISiatConnectivityMonitor monitor,
            IServiceScopeFactory scopeFactory,
            ILogger<ContingencyResendHostedService> logger)
        {
            _monitor = monitor;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _queue = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _monitor.OnRecuperacionDetectada += OnRecuperacion;

            _logger.LogInformation(
                "ContingencyResendHostedService iniciado. Esperando notificaciones del monitor...");

            try
            {
                await foreach (var eventoId in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    await ProcesarEventoAsync(eventoId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "ContingencyResendHostedService detenido por shutdown");
            }
            finally
            {
                _monitor.OnRecuperacionDetectada -= OnRecuperacion;
            }
        }

        private void OnRecuperacion(int codigoSucursal, int codigoPuntoVenta, int eventoId)
        {
            _logger.LogInformation(
                "ContingencyResendHostedService: recuperación detectada (suc={Suc}, pv={PV}, evento={Id}). "
              + "Encolando reenvío.",
                codigoSucursal, codigoPuntoVenta, eventoId);

            // Non-blocking write: el channel es unbounded y el evento es idempotente.
            // Si falla el enqueue (p.ej. channel cerrado durante shutdown) lo logueamos
            // sin tirar el thread del monitor.
            if (!_queue.Writer.TryWrite(eventoId))
            {
                _logger.LogWarning(
                    "No se pudo encolar reenvío de evento {Id} (suc={Suc}, pv={PV}). El reenvío no se procesará automáticamente.",
                    eventoId, codigoSucursal, codigoPuntoVenta);
            }
        }

        private async Task ProcesarEventoAsync(int eventoId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var unit = scope.ServiceProvider.GetRequiredService<IUnitWork>();

            try
            {
                // FIX #6 — si el evento fue Rechazado (típicamente tras cascada 984 al
                // reenviar AutomaticoSinSoap al SIAT), NO paquetizar: el evento no
                // tiene CodigoRecepcionEventoSignificativo, por lo que
                // ReenviarVentasPendientesAsync retornaría early sin tocar nada y las
                // ventas vinculadas quedarían huérfanas para siempre. El flujo de
                // rescate las convierte a online y las reenvía una a una vía
                // EnviarVentaAsync con Facturado=false — el nuevo branch en
                // EnviarVentaAsync llama al Preparer que regenera
                // Cuf/Cufd/NumeroFactura/XmlBase64/Hash con un CUFD vigente del SIAT.
                // Ver [[kafeyana-contingencia-984-rescate]].
                var evento = await unit.eventosSignificativosSiat.FindByIdAsync(eventoId);
                if (evento?.EstadoContingencia == EventoContingenciaEstado.Rechazado)
                {
                    await RescatarVentasDeEventoRechazadoAsync(scope.ServiceProvider, eventoId, ct);
                    return;
                }

                var reenvio = scope.ServiceProvider
                    .GetRequiredService<ReenvioFacturasContingenciaService>();

                // FIX #4: al detectar recuperación, antes de procesar el paquete del
                // evento, barrer las ventas contingencia huérfanas (TipoEmision=2 +
                // EstadoSiat=Pendiente + EventoSignificativoSiatId=null) que quedaron
                // fuera del rescate de VincularVentasPendientesAlEventoAsync
                // (típicamente del período gris, donde el catch de SiatOfflineException
                // mutó la venta antes de que el monitor cruzara el umbral). Las
                // reclasificamos a TipoEmision=1 (en-línea) y las reencolamos online.
                // Ver [[kafeyana-contingencia-984-rescate]].
                await RescatarVentasContingenciaHuerfanasAsync(scope.ServiceProvider, ct);
                var resultado = await reenvio.ReenviarVentasPendientesAsync(eventoId, ct);

                if (resultado.PendientesEncontradas > 0)
                {
                    _logger.LogInformation(
                        "Reenvío contingencia ventas finalizado (eventoId={Id}): "
                      + "pendientes={Total}, validadas={Validadas}, observadas={Observadas}, errores={Errores}",
                        eventoId, resultado.PendientesEncontradas,
                        resultado.Validadas, resultado.Observadas, resultado.Errores);
                }

                // También procesa notas de ajuste contingencia del mismo evento.
                // Si una nota quedó huérfana (Pendiente sin EventoSignificativoSiatId)
                // se vinculó en el DispararContingenciaAutomaticaAsync, pero las
                // notas que se emitieron en línea dentro de la ventana de
                // contingencia necesitan reenvío explícito.
                var resultadoNotas = await reenvio.ReenviarNotasPendientesAsync(eventoId, ct);

                if (resultadoNotas.PendientesEncontradas > 0)
                {
                    _logger.LogInformation(
                        "Reenvío contingencia notas finalizado (eventoId={Id}): "
                      + "pendientes={Total}, validadas={Validadas}, observadas={Observadas}, errores={Errores}",
                        eventoId, resultadoNotas.PendientesEncontradas,
                        resultadoNotas.Validadas, resultadoNotas.Observadas, resultadoNotas.Errores);
                }
            }
            catch (Exception ex)
            {
                // Si falla el reenvío completo (ej: BD caída), NO cerramos el evento:
                // el próximo cobro online re-disparará la detección y el monitor
                // lo reencolará cuando vuelva a detectar recuperación.
                _logger.LogError(ex,
                    "Error al reenviar facturas contingencia (eventoId={Id}). "
                  + "Se reintentará en la próxima recuperación detectada.",
                    eventoId);
            }
        }

        /// <summary>
        /// FIX #4 — rescate de ventas contingencia huérfanas (TipoEmision=2, FK=null).
        /// Tras reclasificarlas a TipoEmision=1 se envían online. Idempotente: si no
        /// hay huérfanas, sale sin tocar la BD.
        /// </summary>
        private async Task RescatarVentasContingenciaHuerfanasAsync(
            IServiceProvider sp,
            CancellationToken ct)
        {
            var ventasRepo = sp.GetRequiredService<KafeYana.Application.IRepositorio.IVentaRepositorio>();
            var envioSiat = sp.GetRequiredService<IFacturaSiatEnvioService>();
            var unitWork = sp.GetRequiredService<KafeYana.Application.IRepositorio.IUnitWork>();

            var huerfanas = await ventasRepo.BuscarPendientesSinEventoAsync(ct);
            if (huerfanas.Count == 0) return;

            _logger.LogWarning(
                "FIX #4: {Count} ventas contingencia huérfanas detectadas (TipoEmision=2, FK=null). "
              + "Reclasificando a TipoEmision=1 y reencolando online.",
                huerfanas.Count);

            foreach (var v in huerfanas)
            {
                // Reclasificar a online para que EnviarVentaAsync tome la rama
                // `EnviarRecepcionAsync` (singular) y no el path contingencia que
                // exige CodigoRecepcionEventoSignificativo.
                // Facturado=false activa el nuevo branch en EnviarVentaAsync que
                // llama al Preparer — regenera Cuf/Cufd/NumeroFactura/XmlBase64/
                // CodigoHash con un CUFD vigente del SIAT (sin esto, el SIAT
                // rechaza con 1002/1003 porque el CUFD viejo ya no es el vigente).
                v.TipoEmision = 1;
                v.EventoSignificativoSiatId = null;
                v.Facturado = false;
                v.ErrorMensaje = "Rescate automático FIX #4: contingencia sin evento activo, reenvío online.";

                try
                {
                    var resultado = await envioSiat.EnviarVentaAsync(v, ct);
                    if (!resultado.Transaccion)
                    {
                        _logger.LogWarning(
                            "FIX #4: Venta {VentaId} rechazada por SIAT en reenvío online: {Msg}",
                            v.Id, resultado.ErrorMensaje);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "FIX #4: Venta {VentaId} reenviada online OK (CodRec={CodRec})",
                            v.Id, resultado.CodigoRecepcion);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "FIX #4: Venta {VentaId} falló al reencolar online. Queda Pendiente.",
                        v.Id);
                }
            }

            await unitWork.SaveUnitWork();
        }

        /// <summary>
        /// FIX #6 — rescate desatendido de ventas vinculadas a un evento Rechazado
        /// (típicamente tras cascada 984 al reenviar AutomaticoSinSoap al SIAT).
        ///
        /// Para cada venta pendiente vinculada:
        ///   TipoEmision=2 → 1
        ///   EventoSignificativoSiatId → null
        ///   Facturado=false (fuerza al preparer a regenerar Cuf/Cufd/NumeroFactura/XmlBase64/CodigoHash)
        ///   ErrorMensaje descriptivo (auditoría)
        /// Luego <see cref="IFacturaSiatEnvioService.EnviarVentaAsync"/> (online) regenera
        /// todo y emite vía <c>recepcionFactura</c> singular (con el fix del 920 ya aplicado).
        ///
        /// Idempotente: si no hay ventas vinculadas, sale sin tocar la BD.
        /// Ver [[kafeyana-contingencia-984-rescate]].
        /// </summary>
        private async Task RescatarVentasDeEventoRechazadoAsync(
            IServiceProvider sp,
            int eventoId,
            CancellationToken ct)
        {
            var ventasRepo = sp.GetRequiredService<IVentaRepositorio>();
            var envioSiat = sp.GetRequiredService<IFacturaSiatEnvioService>();
            var unitWork = sp.GetRequiredService<IUnitWork>();

            var ventas = await ventasRepo.BuscarPendientesPorEventoIdAsync(eventoId, ct);
            if (ventas.Count == 0)
            {
                _logger.LogInformation(
                    "FIX #6: evento {Id} Rechazado, sin ventas contingencia vinculadas para rescatar.",
                    eventoId);
                return;
            }

            _logger.LogWarning(
                "FIX #6: evento {Id} Rechazado. Rescatando {Count} ventas contingencia → online.",
                eventoId, ventas.Count);

            int ok = 0, fail = 0;
            foreach (var v in ventas)
            {
                // Reclasificar a online para que EnviarVentaAsync tome la rama
                // `EnviarRecepcionAsync` (singular) y no el path contingencia que
                // exige CodigoRecepcionEventoSignificativo. Facturado=false activa
                // `PrepararVentaSinFacturarAsync` que regenera TODO y emite online.
                v.TipoEmision = 1;
                v.EventoSignificativoSiatId = null;
                v.Facturado = false;
                v.ErrorMensaje = $"FIX #6: rescate automático — evento {eventoId} Rechazado (984), reenvío online.";

                try
                {
                    var resultado = await envioSiat.EnviarVentaAsync(v, ct);
                    if (resultado.Transaccion)
                    {
                        ok++;
                        _logger.LogInformation(
                            "FIX #6: Venta {VentaId} rescatada online OK (CodRec={CodRec})",
                            v.Id, resultado.CodigoRecepcion);
                    }
                    else
                    {
                        fail++;
                        _logger.LogWarning(
                            "FIX #6: Venta {VentaId} rechazada por SIAT en reenvío online: {Msg}",
                            v.Id, resultado.ErrorMensaje);
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    _logger.LogError(ex,
                        "FIX #6: Venta {VentaId} falló al reenviar online. Queda Pendiente para reintento.",
                        v.Id);
                }
            }

            await unitWork.SaveUnitWork();
            _logger.LogInformation(
                "FIX #6: rescate finalizado para evento {Id}. OK={Ok}, Fail={Fail}, Total={Total}",
                eventoId, ok, fail, ventas.Count);
        }
    }
}