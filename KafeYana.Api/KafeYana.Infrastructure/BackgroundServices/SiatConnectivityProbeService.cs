using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.SiatClient;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Probe periódico de reachability del SIAT. Existe para romper el
    /// chicken-and-egg del monitor de contingencia:
    ///
    /// <para>
    /// Sin este probe, cuando hay una contingencia Activa en memoria, todas
    /// las llamadas SOAP reales caen en el cortocircuito de
    /// <c>SiatHttpClient.EnviarSoapAsync</c> (línea 1532) y el monitor nunca
    /// ve tráfico real que le permita detectar recuperación. Como nadie está
    /// facturando ni sincronizando durante la contingencia, el circuito
    /// queda abierto indefinidamente — cada cobro contingencia crea un
    /// evento nuevo que se atasca con <c>CodigoRecepcion=null</c>.
    /// </para>
    ///
    /// <para>
    /// Este probe hace un HTTP GET barato al <c>BaseAddress</c> cada
    /// <see cref="SiatConnectivityProbeOptions.IntervaloSegundos"/>. NO
    /// pasa por el cortocircuito (usa <c>SiatHttpClient.PingSiatAsync</c>,
    /// que es una vía directa a <c>HttpClient.GetAsync</c>). Si SIAT
    /// responde cualquier HTTP status, llama
    /// <c>monitor.ReportarExitoAsync("Cuis", suc, pv, ct)</c>:
    /// </para>
    ///
    /// <list type="number">
    ///   <item>El monitor ve la operación como crítica (Cuis) y la contingencia
    ///         como Activa → llama Pieza 3b (<c>ReenviarRegistroAsync</c>).</item>
    ///   <item>Pieza 3b hace el SOAP real a SIAT para obtener
    ///         <c>CodigoRecepcionEventoSignificativo</c> y persiste el UPDATE.</item>
    ///   <item>Monitor cierra contingencia → dispara
    ///         <see cref="ISiatConnectivityMonitor.OnRecuperacionDetectada"/>.</item>
    ///   <item><c>ContingencyResendHostedService</c> reenvía las Facturas pendientes.</item>
    ///   <item>El cortocircuito se desactiva y los sincronizadores vuelven a
    ///         hablar con SIAT normalmente.</item>
    /// </list>
    ///
    /// <para>
    /// El probe SOLO itera sobre los pares (suc, pv) que el monitor está
    /// trackeando actualmente (típicamente los que tienen contingencia
    /// activa). Si no hay contingencias, el ciclo no hace ningún HTTP
    /// request — cero overhead en operación normal.
    /// </para>
    /// </summary>
    public class SiatConnectivityProbeService : BackgroundService
    {
        private readonly SiatHttpClient _siat;
        private readonly ISiatConnectivityMonitor _monitor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SiatOptions _siatOpts;
        private readonly SiatConnectivityProbeOptions _opts;
        private readonly ILogger<SiatConnectivityProbeService> _logger;

        public SiatConnectivityProbeService(
            SiatHttpClient siat,
            ISiatConnectivityMonitor monitor,
            IServiceScopeFactory scopeFactory,
            IOptions<SiatOptions> siatOpts,
            IOptions<SiatConnectivityProbeOptions> opts,
            ILogger<SiatConnectivityProbeService> logger)
        {
            _siat = siat;
            _monitor = monitor;
            _scopeFactory = scopeFactory;
            _siatOpts = siatOpts.Value;
            _opts = opts.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_opts.Habilitado)
            {
                _logger.LogInformation(
                    "SiatConnectivityProbeService deshabilitado por configuración. No se ejecutará.");
                return;
            }

            // Demora inicial: le da tiempo a ContingencyBootstrapHostedService
            // a hidratar el monitor desde BD antes de empezar a probar.
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_opts.DemoraInicialSegundos),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _logger.LogInformation(
                "SiatConnectivityProbeService iniciado. Intervalo={Intervalo}s, Timeout={Timeout}s.",
                _opts.IntervaloSegundos, _opts.TimeoutSegundos);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EjecutarCicloAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Cualquier excepción inesperada (no cancellation) la logueamos
                    // pero seguimos vivos. El próximo ciclo reintenta.
                    _logger.LogError(ex,
                        "SiatConnectivityProbeService: error inesperado en ciclo. Se reintentará en el próximo intervalo.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_opts.IntervaloSegundos),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("SiatConnectivityProbeService detenido por shutdown.");
        }

        private async Task EjecutarCicloAsync(CancellationToken ct)
        {
            // Sólo probamos pares que están actualmente en contingencia
            // (los que necesitan recovery detection). Si no hay ninguno, el
            // ciclo termina sin tráfico de red.
            var pares = _monitor.ObtenerParesMonitoreados();
            if (pares.Count == 0)
            {
                _logger.LogInformation(
                    "SiatConnectivityProbeService: sin pares en contingencia. Nada que probar.");
                return;
            }

            _logger.LogInformation(
                "SiatConnectivityProbeService: probando {N} par(es) en contingencia...",
                pares.Count);

            var timeout = TimeSpan.FromSeconds(_opts.TimeoutSegundos);

            // Un scope por ciclo: ICuisService es Scoped (usa EF Core / BD).
            // No podemos inyectarlo directamente en un BackgroundService
            // (Singleton). El scope se cierra automáticamente al salir del
            // bloque using.
            using var scope = _scopeFactory.CreateScope();
            var cuisService = scope.ServiceProvider.GetRequiredService<ICuisService>();

            foreach (var (suc, pv) in pares)
            {
                if (ct.IsCancellationRequested) break;

                // Cada ping tiene su propio timeout independiente — si un par
                // está colgado, no bloquea el resto del ciclo.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeout);

                bool alive;
                try
                {
                    alive = await ProbeSiatAliveAsync(cuisService, suc, pv, cts.Token);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    // Timeout del ping individual, no del shutdown.
                    alive = false;
                }

                if (alive)
                {
                    _logger.LogInformation(
                        "SiatConnectivityProbeService: SIAT alcanzable en (suc={Suc}, pv={PV}). "
                      + "Reportando éxito para disparar recuperación.",
                        suc, pv);

                    // Activar bypass GLOBAL del cortocircuito ANTES de
                    // ReportarExitoAsync. Esto evita que el stoppingToken del
                    // probe se propague por toda la cadena de llamadas SOAP
                    // (que era la causa de las OperationCanceledException
                    // durante SaveChangesAsync y del bucle cada 60s).
                    _monitor.ActivarBypass(suc, pv);

                    // Limpiar guard Pieza3bFalloUtc (si quedó seteado por un
                    // fallo previo: servicio mal mapeado, timeout de BD, error
                    // de configuración). Ahora que el guard de re-entrada
                    // (Pieza3bEnProgreso) previene el bucle recursivo, es
                    // SEGURO limpiar este flag cuando el SIAT responde OK:
                    // si el reintento falla de nuevo, sólo se setea otra vez
                    // hasta el próximo ciclo del probe (60s después) — no hay
                    // bucle, sólo ruido en el log.
                    _monitor.LimpiarPieza3bFallo(suc, pv);

                    // "Cuis" es operación crítica en EsOperacionCritica
                    // (SiatConnectivityMonitor.cs:546-549) → ReportarExitoAsync
                    // detecta contingencia activa y dispara Pieza 3b + cierre
                    // + OnRecuperacionDetectada. Idempotente.
                    try
                    {
                        await _monitor.ReportarExitoAsync(
                            SiatOperacion.Cuis, suc, pv, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "SiatConnectivityProbeService: ReportarExitoAsync falló "
                          + "para (suc={Suc}, pv={PV}). Se reintentará en el próximo ciclo.",
                            suc, pv);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "SiatConnectivityProbeService: SIAT no alcanzable en (suc={Suc}, pv={PV}).",
                        suc, pv);
                }
            }
        }

        /// <summary>
        /// Health check real contra el SIAT: ejecuta un SOAP <c>verificarNit</c>
        /// con el CUIS vigente del par (suc, pv). El flag <c>bypassCortocircuito</c>
        /// evita que el monitor se ciegue a sí mismo (este es justamente el
        /// mecanismo que reabre el circuito). Devuelve <c>true</c> sólo cuando
        /// SIAT responde <c>transaccion=true</c> — un HTTP 503 o SOAP fault
        /// cuenta como caído.
        ///
        /// Fallback: si no hay CUIS en cache (boot, BD recién limpiada, etc.),
        /// cae a un HTTP GET barato al BaseAddress. Menos confiable pero útil
        /// para detectar recuperación cuando aún no tenemos credenciales.
        /// </summary>
        private async Task<bool> ProbeSiatAliveAsync(
            ICuisService cuisService, int suc, int pv, CancellationToken ct)
        {
            try
            {
                var cuis = await cuisService.ObtenerCuisVigenteAsync(suc, pv, ct);
                if (cuis is not null && !string.IsNullOrWhiteSpace(cuis.Codigo))
                {
                    // El cortocircuito NO se aplica aquí porque esta llamada
                    // es justamente la que detecta recuperación. Si el monitor
                    // tuviera la contingencia Activa, SIATOfflineException
                    // bloquearía este check — por eso VerificarNitAsync tiene
                    // bypass interno (verificarNit es la primera señal que
                    // detecta el cortocircuito y por eso el SiatHttpClient
                    // consulta monitor.TieneBypassActivo antes de cortocircuitar).
                    var resp = await _siat.VerificarNitAsync(
                        nitAVerificar: _siatOpts.Nit,
                        cuis: cuis.Codigo,
                        codigoSucursal: suc,
                        ct: ct);

                    return resp.Transaccion;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelación del shutdown — propagamos al caller (que ya la maneja).
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "SiatConnectivityProbeService: verificarNit falló en (suc={Suc}, pv={PV}). "
                  + "Cayendo a HTTP GET fallback.",
                    suc, pv);
                // Caemos al fallback HTTP GET abajo.
            }

            // Fallback: HTTP GET al BaseAddress. Devuelve vivo si status < 500
            // (2xx éxito, 3xx redirect, 4xx método/recurso no existe — el endpoint
            // SOAP no acepta GET, eso es normal). HTTP 5xx (503 Service Unavailable,
            // 502, 504) cuenta como caído — SIAT responde pero el servicio no
            // está disponible. Un "vivo" falso dispararía ReenviarRegistroAsync
            // que el SIAT realmente caído rechaza con [984]. Ver memoria
            // kafeyana-probe-503-clasificacion. Timeout / DNS / connection
            // refused también cuenta como caído.
            return await _siat.PingSiatAsync(ct);
        }
    }
}
