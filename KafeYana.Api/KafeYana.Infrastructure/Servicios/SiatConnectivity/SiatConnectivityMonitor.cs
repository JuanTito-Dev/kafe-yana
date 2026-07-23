using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.SiatConnectivity
{
    /// <summary>
    /// Implementación singleton del monitor de conectividad SIAT.
    /// Ver <see cref="ISiatConnectivityMonitor"/> para el contrato y
    /// [[kafeyana-contingencia-siat]] para el flujo normativo completo.
    ///
    /// Estado interno:
    /// <list type="bullet">
    ///   <item><c>_estados</c>: <c>ConcurrentDictionary&lt;(int,int), EstadoConexion&gt;</c>
    ///         con contadores de fallos y el evento activo por PV.</item>
    ///   <item><c>_locks</c>: <c>ConcurrentDictionary&lt;(int,int), SemaphoreSlim&gt;</c>
    ///         para serializar el disparo de contingencia automática por PV
    ///         (evita doble registro cuando múltiples requests fallan en simultáneo).</item>
    /// </list>
    /// </summary>
    public class SiatConnectivityMonitor : ISiatConnectivityMonitor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<DetectorOptions> _opts;
        private readonly SiatOptions _siatOpts;
        private readonly ILogger<SiatConnectivityMonitor> _logger;

        private readonly ConcurrentDictionary<(int suc, int pv), EstadoConexion> _estados = new();
        private readonly ConcurrentDictionary<(int suc, int pv), SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<(int suc, int pv), bool> _bypassActivo = new();

        public event Action<int, int, int>? OnRecuperacionDetectada;

        public SiatConnectivityMonitor(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<DetectorOptions> opts,
            IOptions<SiatOptions> siatOpts,
            ILogger<SiatConnectivityMonitor> logger)
        {
            _scopeFactory = scopeFactory;
            _opts = opts;
            _siatOpts = siatOpts.Value;
            _logger = logger;
        }

        public async Task InicializarAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var eventoSvc = scope.ServiceProvider
                .GetRequiredService<IEventoSignificativoSiatService>();

            var contingencias = await eventoSvc.ListarContingenciasActivasAsync(ct);

            // Gap 6: detectar eventos activos con FechaHoraInicio demasiado vieja.
            // El SIN rechazará su registro con error 981 si intentamos reenviarlos,
            // así que los auto-expiramos al boot en lugar de esperar al fallo
            // SOAP. Es un proxy pre-emptivo para evitar ruido operacional.
            int autoExpiradas = 0;
            var horasLimite = _siatOpts.HorasMaximaContingenciaAbierta;
            var fechaCorte = DateTime.UtcNow.AddHours(-horasLimite);

            foreach (var c in contingencias)
            {
                var key = (c.CodigoSucursal, c.CodigoPuntoVenta);

                if (c.FechaHoraInicioEvento < fechaCorte)
                {
                    try
                    {
                        var ventasAfectadas = await eventoSvc.AutoExpirarEventoAsync(
                            c.EventoSignificativoId, ct);
                        autoExpiradas++;
                        _logger.LogInformation(
                            "InicializarAsync: evento {Id} auto-expirado "
                          + "(FechaHoraInicio={FechaInicio:O} > {Horas}h de antigüedad, "
                          + "{N} ventas marcadas con error).",
                            c.EventoSignificativoId, c.FechaHoraInicioEvento,
                            horasLimite, ventasAfectadas);
                    }
                    catch (Exception ex)
                    {
                        // No bloquear el boot si la auto-expiración de un evento
                        // falla. Se loguea y se continúa. El evento sigue Activo
                        // en BD y será capturado por el guard de Pieza 3b (Gap 5)
                        // si el monitor intenta reenviarlo y el SIAT devuelve 981.
                        _logger.LogError(ex,
                            "InicializarAsync: error auto-expirando evento {Id}. "
                          + "Continúa como Activo en cache.",
                            c.EventoSignificativoId);
                    }

                    // No agregar al cache de pendientes — el evento es terminal.
                    continue;
                }

                _estados[key] = new EstadoConexion
                {
                    FallosConsecutivos = 0,
                    PrimerFalloUtc = null,
                    EventoSignificativoId = c.EventoSignificativoId,
                    CodigoRecepcionEventoSignificativo = c.CodigoRecepcionEventoSignificativo,
                    Origen = c.Origen,
                    ContingenciaActiva = true
                };
            }

            if (autoExpiradas > 0)
            {
                _logger.LogInformation(
                    "InicializarAsync: {N} eventos auto-expirados por exceder {H}h "
                  + "de antigüedad. Las ventas asociadas requieren acción manual.",
                    autoExpiradas, horasLimite);
            }

            // Gap 7: sweep de ventas contingencia con CUF malformado por bug pre-fix.
            // Concatenaba el CUFD base64 como CodigoControl en vez del hex. El CUF
            // resultante terminaba con base64 y SIAT lo rechaza por algoritmo inválido.
            // Marcamos las huérfanas con ErrorMensaje pidiendo anulación manual. No se
            // regenera el CUF retroactivamente (riesgo de 1002/1003 por fecha mal
            // reconstruida). Idempotente: WHERE ErrorMensaje IS NULL evita re-marcar.
            try
            {
                var mensajeErrorCuf =
                    "CUF malformado por bug de versión pre-Gap-7 (se concatenaba CUFD "
                  + "base64 en lugar del CodigoControl hex del CUFD). El SIAT rechazará "
                  + "esta factura al reenviarla por CUF algorítmicamente inválido. "
                  + "Requiere ANULACIÓN MANUAL desde la UI o desvincular el evento. "
                  + "Las nuevas ventas contingencia NO presentan este bug.";

                int marcadas;
                using (var scopeSweep = _scopeFactory.CreateScope())
                {
                    var repo = scopeSweep.ServiceProvider
                        .GetRequiredService<IVentaRepositorio>();
                    marcadas = await repo.MarcarVentasCufMalformadoAsync(
                        mensajeErrorCuf, ct);
                }

                if (marcadas > 0)
                {
                    _logger.LogWarning(
                        "InicializarAsync (Gap 7): {N} ventas contingencia con CUF malformado "
                      + "marcadas con ErrorMensaje. Requieren anulación manual.",
                        marcadas);
                }
            }
            catch (Exception ex)
            {
                // No bloquear el boot si el sweep falla. Las ventas quedan Pendientes
                // y eventualmente el monitor las reintentará; el reenvío seguirá
                // fallando (CUF malformado) y SIAT las rechazará con error claro.
                _logger.LogError(ex,
                    "InicializarAsync (Gap 7): error en sweep de CUF malformado.");
            }

            _logger.LogInformation(
                "Monitor de conectividad inicializado. Contingencias activas en BD: {N} "
              + "(estados Activos solamente; Cerrados, Rechazados y AutoExpirados son ignorados por el repositorio).",
                _estados.Count(kv => kv.Value.ContingenciaActiva));
        }

        public int? ObtenerEventoActivo(int codigoSucursal, int codigoPuntoVenta) =>
            _estados.TryGetValue((codigoSucursal, codigoPuntoVenta), out var estado)
                ? estado.EventoSignificativoId
                : null;

        public bool EstaEnLinea(int codigoSucursal, int codigoPuntoVenta) =>
            ObtenerEventoActivo(codigoSucursal, codigoPuntoVenta) is null;

        public void LimpiarEstadoContingencia(int codigoSucursal, int codigoPuntoVenta)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            _estados.TryRemove(key, out _);
            _bypassActivo.TryRemove(key, out _);
            _logger.LogInformation(
                "Estado de contingencia limpiado en monitor. Suc={Suc}, PV={PV}",
                key.suc, key.pv);
        }

        public void NotificarContingenciaExterna(int codigoSucursal, int codigoPuntoVenta, int eventoId)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            var estado = _estados.GetOrAdd(key, _ => new EstadoConexion());
            if (!estado.ContingenciaActiva)
            {
                estado.ContingenciaActiva = true;
                estado.EventoSignificativoId = eventoId;
                estado.Origen = "Manual";
                _logger.LogInformation(
                    "Contingencia manual registrada en monitor (endpoint). "
                  + "Suc={Suc}, PV={PV}, EventoId={Id}",
                    key.suc, key.pv, eventoId);
            }
        }

        public void ActivarBypass(int codigoSucursal, int codigoPuntoVenta)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            _bypassActivo[key] = true;
            _logger.LogInformation(
                "Bypass del cortocircuito ACTIVADO. Suc={Suc}, PV={PV}",
                key.suc, key.pv);
        }

        public void LimpiarPieza3bFallo(int codigoSucursal, int codigoPuntoVenta)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            if (_estados.TryGetValue(key, out var estado)
                && estado.Pieza3bFalloUtc is not null)
            {
                _logger.LogInformation(
                    "Limpiando guard Pieza3bFalloUtc para (suc={Suc}, pv={PV}). "
                  + "Permitirá UN reintento de Pieza 3b en este ciclo.",
                    key.suc, key.pv);
                estado.Pieza3bFalloUtc = null;
            }
        }

        public bool TieneBypassActivo(int codigoSucursal, int codigoPuntoVenta) =>
            _bypassActivo.TryGetValue((suc: codigoSucursal, pv: codigoPuntoVenta), out var activo) && activo;

        public IReadOnlyList<(int Sucursal, int PuntoVenta)> ObtenerParesMonitoreados()
        {
            // Snapshot inmutable: si una contingencia se cierra durante la
            // iteración del probe, el par seguirá apareciendo en este listado
            // (el snapshot es de la enumeración original). El probe solo prueba
            // pares que están caídos; un éxito espurio no tiene副作用 negativo
            // porque ReportarExitoAsync es idempotente.
            return _estados.Keys
                .Select(k => (Sucursal: k.suc, PuntoVenta: k.pv))
                .ToList();
        }

        public void PublicarRecuperacion(int codigoSucursal, int codigoPuntoVenta, int eventoId)
        {
            // Disparado por el bootstrap al arrancar para "sincronizar" el
            // estado del monitor con contingencias que ya existían en BD.
            // NO cierra el evento (la BD ya lo tiene Activo, lo cerrará
            // ReportarExitoAsync cuando se observe tráfico online real) —
            // solo encola el reenvío para que las ventas pendientes no queden
            // esperando al próximo cobro que toque el SIAT.
            _logger.LogInformation(
                "PublicarRecuperacion (bootstrap): suc={Suc}, pv={PV}, eventoId={Id}",
                codigoSucursal, codigoPuntoVenta, eventoId);

            // Guard: si el evento fue persistido sin codigoRecepcion (origen
            // AutomaticoSinSoap — Pieza 2 fallback), NO disparar resend todavía.
            // Las ventas/notas contingencia del evento fallarían con
            // "Falta codigoRecepcionEventoSignificativo" porque el SIAT no
            // puede asociarlas a un evento sin código. El probe debe completar
            // Pieza 3b primero — cuando ReportarExitoAsync("Cuis"...) se
            // dispara con tráfico real al SIAT, ReenviarRegistroAsync poblará
            // codigoRecepcion y entonces sí fire OnRecuperacionDetectada.
            //
            // Ver [[kafeyana-contingencia-siat]].
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            if (_estados.TryGetValue(key, out var estadoBootstrap)
                && estadoBootstrap.EventoSignificativoId == eventoId
                && string.IsNullOrEmpty(estadoBootstrap.CodigoRecepcionEventoSignificativo))
            {
                _logger.LogInformation(
                    "PublicarRecuperacion: evento {Id} sin codigoRecepcion (origen={Origen}). "
                  + "NO se dispara resend — el probe completará Pieza 3b primero.",
                    eventoId, estadoBootstrap.Origen);
                return;
            }

            OnRecuperacionDetectada?.Invoke(codigoSucursal, codigoPuntoVenta, eventoId);
        }

        public async Task ReportarExitoAsync(
            string operacion,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);

            // Solo nos importan las operaciones críticas para el umbral y la
            // detección de recuperación.
            if (!EsOperacionCritica(operacion))
                return;

            var estado = _estados.GetOrAdd(key, _ => new EstadoConexion());

            // Éxito: reseteamos el contador de fallos.
            if (estado.FallosConsecutivos > 0)
            {
                _logger.LogDebug(
                    "SIAT respondió OK a {Op} después de {N} fallos. Reseteando contador. Suc={Suc}, PV={PV}",
                    operacion, estado.FallosConsecutivos, key.suc, key.pv);
            }
            estado.FallosConsecutivos = 0;
            estado.PrimerFalloUtc = null;

            // Si había contingencia activa Y el éxito vino de una operación que
            // ya estaba activa cuando arrancó el corte (no una sync de catálogo
            // iniciada justo después), disparamos recuperación.
            if (!estado.ContingenciaActiva || estado.EventoSignificativoId is null)
                return;

            // Recuperación: la declaramos solo cuando el éxito proviene de una
            // operación crítica del path de venta, no de la sync diaria.
            if (operacion != SiatOperacion.Cuis
                && operacion != SiatOperacion.Cufd
                && operacion != SiatOperacion.FechaHora
                && operacion != SiatOperacion.RecepcionFactura
                // FIX #1: validacionRecepcionPaqueteFactura también es crítica para
                // el flujo contingencia — su éxito confirma que el SIAT está vivo
                // para cerrar paquetes contingencia.
                && operacion != SiatOperacion.ValidacionRecepcionPaqueteFactura)
            {
                return;
            }

            // Esperar anti-flapping antes de declarar recuperación.
            var eventoId = estado.EventoSignificativoId.Value;
            var codigoRecepcion = estado.CodigoRecepcionEventoSignificativo;

            // Guard de re-entrada: si ya hay un ReenviarRegistroAsync en curso
            // para este (suc, pv), retornamos inmediato. Esto evita la recursión
            // desde los SOAPs exitosos del propio flujo de Pieza 3b:
            // EnviarSoapAsync llama ReportarExitoAsync tras cada SOAP crítico
            // exitoso (SolicitarCuisAsync, SolicitarCufdAsync,
            // RegistroEventoSignificativoAsync). Sin este guard, cada SOAP
            // dispara OTRA instancia del helper, generando N llamadas
            // concurrentes que saturan el threadpool, la BD y el HttpClient.
            if (estado.Pieza3bEnProgreso)
            {
                _logger.LogDebug(
                    "Pieza 3b ya en progreso para evento {Id}. Re-entrada ignorada.",
                    eventoId);
                return;
            }

            // Guard de no-retry: si Pieza 3b ya falló en este ciclo de vida del
            // monitor, NO reintentamos cada 60s (bucle infinito de probe →
            // Pieza 3b → fail → 60s → repeat). Sólo se desbloquea con un restart
            // del backend (estado se reconstruye desde BD en InicializarAsync)
            // o cuando el operador intervenga manualmente.
            if (estado.Pieza3bFalloUtc is not null)
            {
                _logger.LogWarning(
                    "Pieza 3b ya falló a las {Ts} para evento {Id}. NO se reintenta. "
                  + "Requiere restart del backend o intervención manual para reintentar.",
                    estado.Pieza3bFalloUtc, eventoId);
                return;
            }

            // Pieza 3 — si el evento fue persistido sin codigoRecepcion
            // (origen AutomaticoSinSoap desde la Pieza 2), reenviar al SIAT
            // primero para obtener el codigoRecepcion real. Si falla, NO cerrar
            // contingencia ni disparar reenvío de facturas (Opción B confirmada).
            //
            // El bloque está envuelto en try/finally que setea
            // Pieza3bEnProgreso=true ANTES de iniciar el helper y
            // =false en finally (incluso si la operación falla) para que
            // llamadas re-entrantes desde SOAPs exitosos internos sean
            // rechazadas con LogDebug y NO disparen otra instancia.
            if (string.IsNullOrEmpty(codigoRecepcion) || codigoRecepcion.Trim() == "0")
            {
                _logger.LogInformation(
                    "Pieza 3: evento {Id} sin codigoRecepcion válido (valor='{Valor}'). "
                  + "Reenviando al SIAT antes de cerrar contingencia.",
                    eventoId, codigoRecepcion ?? "(null)");

                estado.Pieza3bEnProgreso = true;
                try
                {
                    var codigoRecepcionNuevo = await ReenviarRegistroAsync(
                        eventoId, key.suc, key.pv, ct);

                    if (string.IsNullOrEmpty(codigoRecepcionNuevo) || codigoRecepcionNuevo.Trim() == "0")
                    {
                        // Bug fixed (jun-2026): el monitor cachea el ID del
                        // evento desde el boot (o desde Pieza 2 fallback) en
                        // `_estados`. Si entremedio Pieza 3b falló y el servicio
                        // persistió el evento como Rechazado o Cerrado, el cache
                        // queda desincronizado: el monitor sigue viendo
                        // `codRecep=null` y reintenta cada ~60s cuando el probe
                        // limpia el flag Pieza3bFalloUtc antes de ReportarExito.
                        //
                        // Antes de marcar para NO reintentar, refrescamos desde
                        // BD. Si el evento ya está en estado terminal (Rechazado
                        // o Cerrado), limpiamos `_estados` y salimos sin
                        // setear Pieza3bFalloUtc — el evento NO necesita
                        // reenvío, es terminal.
                        // FIX #6 (post-mortem jun-2026): lookup por eventoId,
                        // NO por (suc,pv)+Activo. Tras cascada 984 el evento
                        // queda Rechazado; ObtenerContingenciaActivaAsync filtra
                        // por Activo y devolvería null, ocultando la rama de
                        // FIX #6 que dispara el rescate desatendido.
                        var estadoBd = await ObtenerEstadoContingenciaAsync(eventoId, ct);

                        if (estadoBd is null)
                        {
                            // FIX #6: con lookup por eventoId este null ya NO
                            // es "el evento pasó a Rechazado" (eso lo cubre la
                            // rama de abajo). Aquí sólo llegamos si el evento
                            // fue borrado manualmente de la BD. Caso raro:
                            // limpiar cache y salir.
                            _logger.LogWarning(
                                "Pieza 3: evento {Id} no encontrado en BD (borrado manualmente?). Limpiando cache.",
                                eventoId);
                            LimpiarEstadoContingencia(key.suc, key.pv);
                            return;
                        }

                        if (estadoBd.EstadoContingencia == EventoContingenciaEstado.Rechazado)
                        {
                            // FIX #6 — distinguir Rechazado de Cerrado. Si fue
                            // Rechazado (típicamente 984 cascade del resend de un
                            // AutomaticoSinSoap), las ventas contingencia
                            // vinculadas están huérfanas. Limpiamos cache del
                            // monitor Y disparamos OnRecuperacionDetectada:
                            // ContingencyResendHostedService.ProcesarEventoAsync
                            // verá EstadoContingencia=Rechazado y ejecutará
                            // RescatarVentasDeEventoRechazadoAsync automáticamente.
                            // Ver [[kafeyana-contingencia-984-rescate]].
                            _logger.LogWarning(
                                "Pieza 3: evento {Id} Rechazado en BD (típicamente 984 cascade). "
                              + "Disparando rescate desatendido FIX #6 de las ventas vinculadas.",
                                eventoId);
                            LimpiarEstadoContingencia(key.suc, key.pv);
                            OnRecuperacionDetectada?.Invoke(key.suc, key.pv, eventoId);
                            return;
                        }

                        if (estadoBd.EstadoContingencia == EventoContingenciaEstado.Cerrado)
                        {
                            // Cerrado = evento procesado OK previamente, las ventas
                            // ya fueron reenviadas y validadas. No hay nada que
                            // rescatar: solo limpiar cache.
                            _logger.LogWarning(
                                "Pieza 3: evento {Id} ya está Cerrado en BD "
                              + "(terminal). Limpiando cache del monitor. "
                              + "NO se reintenta — la contingencia ya está cerrada.",
                                eventoId);
                            LimpiarEstadoContingencia(key.suc, key.pv);
                            return;
                        }

                        // Estado sigue Activo: fallo transitorio del SOAP/BD.
                        // Marcar para NO reintentar este ciclo y esperar el
                        // próximo (~60s).
                        estado.Pieza3bFalloUtc = DateTime.UtcNow;
                        _logger.LogError(
                            "Pieza 3: ReenviarRegistroAsync falló para evento {Id}. "
                          + "NO se cierra contingencia, NO se reenvían facturas. "
                          + "Marcado para NO reintentar. Requiere restart del backend "
                          + "o intervención manual para reintentar.",
                            eventoId);
                        return;
                    }

                    codigoRecepcion = codigoRecepcionNuevo;
                    estado.CodigoRecepcionEventoSignificativo = codigoRecepcion;
                }
                finally
                {
                    estado.Pieza3bEnProgreso = false;
                }
            }

            // Contingencia manual: el operador la abrió desde la API y la cierra
            // explícitamente. El probe NO debe cerrarla automáticamente — si lo hace,
            // las ventas 2+ quedan sin evento activo y se facturan online (Bug 1).
            // Solo dejar de monitorear el par y disparar el resend de pendientes.
            if (estado.Origen == "Manual")
            {
                _logger.LogInformation(
                    "ReportarExito: contingencia manual (evento {Id}, suc={Suc}, pv={PV}). "
                  + "No se auto-cierra. Disparando resend de pendientes.",
                    eventoId, key.suc, key.pv);
                LimpiarEstadoContingencia(key.suc, key.pv);
                OnRecuperacionDetectada?.Invoke(key.suc, key.pv, eventoId);
                return;
            }

            await CerrarContingenciaAsync(eventoId, key.suc, key.pv, codigoRecepcion, ct);

            LimpiarEstadoContingencia(key.suc, key.pv);
            OnRecuperacionDetectada?.Invoke(key.suc, key.pv, eventoId);
        }

        /// <summary>
        /// Helper privado del monitor. Llama a
        /// <c>IEventoSignificativoSiatService.ReenviarRegistroAsync</c> dentro
        /// de un scope nuevo (el monitor es Singleton, los servicios son Scoped).
        /// Devuelve el codigoRecepcionEventoSignificativo real si OK, o null
        /// si falló. La excepción NO se propaga — se loguea dentro.
        /// </summary>
        private async Task<string?> ReenviarRegistroAsync(
            int eventoId,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct)
        {
            try
            {
                // CTS dedicado con timeout de 60s — desacoplado del stoppingToken
                // del probe (BackgroundService). El flujo de Pieza 3b toca SOAP
                // (SolicitarCufdAsync + RegistroEventoSignificativoAsync) y BD
                // (SaveChangesAsync); si alguno supera los 60s, queremos cancelar
                // con un error claro en vez de propagar cancellation accidental
                // desde el probe.
                using var ctsPieza3b = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsPieza3b.CancelAfter(TimeSpan.FromSeconds(60));

                using var scope = _scopeFactory.CreateScope();
                var eventoSvc = scope.ServiceProvider
                    .GetRequiredService<IEventoSignificativoSiatService>();

                var resultado = await eventoSvc.ReenviarRegistroAsync(
                    eventoId, ctsPieza3b.Token);
                return resultado.CodigoRecepcionEventoSignificativo;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Cancelación por timeout interno de Pieza 3b (no del shutdown).
                _logger.LogError(
                    "Pieza 3 — ReenviarRegistroAsync cancelado por timeout (60s). "
                  + "EventoId={Id}, Suc={Suc}, PV={PV}",
                    eventoId, codigoSucursal, codigoPuntoVenta);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Pieza 3 — ReenviarRegistroAsync lanzó excepción. EventoId={Id}, Suc={Suc}, PV={PV}",
                    eventoId, codigoSucursal, codigoPuntoVenta);
                return null;
            }
        }

        /// <summary>
        /// Helper privado del monitor: refresca el estado del evento desde BD
        /// para sincronizar el cache `_estados` con la verdad de la BD.
        ///
        /// Usado por Pieza 3b después de un fallo de <c>ReenviarRegistroAsync</c>:
        /// si el evento ya está Rechazado o Cerrado en BD, el cache del monitor
        /// quedó desincronizado (cacheó el ID cuando estaba Activo, pero el
        /// servicio persistió el Rechazado/Cerrado después). Sin este refresh,
        /// el monitor reintenta cada ~60s indefinidamente.
        ///
        /// FIX #6 (post-mortem jun-2026): el lookup es POR <paramref name="eventoId"/>
        /// y NO por (suc, pv) + EstadoContingencia=Activo. La razón es que
        /// <c>ObtenerContingenciaActivaAsync</c> filtra por Activo, y justo
        /// cuando necesitamos ver el Rechazado (cascada 984) el evento ya no
        /// está Activo — el lookup devolvería null y la rama "no encontrado
        /// en BD" enmascararía la Rechazado, perdiendo el disparo del rescate
        /// desatendido. Mirar por ID recupera el evento en cualquier estado
        /// terminal (Activo/Rechazado/Cerrado/AutoExpirado) para que las
        /// ramas de decisión siguientes funcionen correctamente.
        ///
        /// Devuelve un <see cref="SnapshotEventoContingencia"/> con los campos
        /// mínimos para decidir, o null si el evento no existe en BD
        /// (caso raro: fue borrado manualmente).
        /// </summary>
        private async Task<SnapshotEventoContingencia?> ObtenerEstadoContingenciaAsync(
            int eventoId,
            CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IUnitWork>();

            var entity = await db.eventosSignificativosSiat
                .FindByIdAsync(eventoId);

            if (entity is null) return null;

            return new SnapshotEventoContingencia
            {
                Id = entity.Id,
                EstadoContingencia = entity.EstadoContingencia,
                CodigoRecepcionEventoSignificativo = entity.CodigoRecepcionEventoSignificativo
            };
        }

        /// <summary>
        /// Snapshot mínimo del evento para decisiones de cache del monitor.
        /// </summary>
        private sealed class SnapshotEventoContingencia
        {
            public int Id { get; set; }
            public string EstadoContingencia { get; set; } = string.Empty;
            public string? CodigoRecepcionEventoSignificativo { get; set; }
        }

        public async Task ReportarFalloAsync(
            string operacion,
            Exception ex,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);

            // Guard de re-entrada: el monitor NO debe contar los fallos de las
            // operaciones de contingencia (RegistroEventoSignificativo, CierreEvento)
            // porque eso podría generar un loop infinito si el SIAT sigue caído.
            if (operacion == SiatOperacion.RegistroEventoSignificativo
                || operacion == SiatOperacion.CierreEvento)
            {
                return;
            }

            // Solo cuentan las operaciones críticas.
            if (!EsOperacionCritica(operacion))
                return;

            var estado = _estados.GetOrAdd(key, _ => new EstadoConexion());
            var ahora = DateTime.UtcNow;

            // Si ya pasó más tiempo que la ventana desde el primer fallo, reseteamos.
            if (estado.PrimerFalloUtc is DateTime primer
                && (ahora - primer).TotalSeconds > _opts.CurrentValue.VentanaFallosSegundos)
            {
                estado.FallosConsecutivos = 0;
                estado.PrimerFalloUtc = null;
            }

            estado.FallosConsecutivos++;
            estado.PrimerFalloUtc ??= ahora;

            _logger.LogWarning(
                "SIAT {Op} falló ({ExType}: {ExMsg}). Fallos consecutivos: {N}/{Umbral}. Suc={Suc}, PV={PV}",
                operacion, ex.GetType().Name, ex.Message,
                estado.FallosConsecutivos, _opts.CurrentValue.UmbralFallosConsecutivos, key.suc, key.pv);

            // Si todavía no llegamos al umbral, no disparamos nada.
            if (estado.FallosConsecutivos < _opts.CurrentValue.UmbralFallosConsecutivos)
                return;

            // Si ya hay contingencia activa para este PV, no abrimos otra.
            if (estado.ContingenciaActiva)
                return;

            // Anti-flapping: si cerramos una contingencia hace poco, no abrimos otra.
            // Ver [[kafeyana-contingencia-siat]] sección riesgos.
            // (Por ahora la implementación es "una contingencia activa por PV"
            //  garantizada por EventoSignificativoSiatService.ObtenerContingenciaActivaAsync;
            //  el diccionario interno refleja la realidad.)

            await DispararContingenciaAutomaticaAsync(key.suc, key.pv, estado, ct);
        }

        private async Task DispararContingenciaAutomaticaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            EstadoConexion estado,
            CancellationToken ct)
        {
            var key = (suc: codigoSucursal, pv: codigoPuntoVenta);
            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            bool tomado = false;
            try
            {
                tomado = await sem.WaitAsync(TimeSpan.FromSeconds(2), ct);
                if (!tomado)
                {
                    _logger.LogWarning(
                        "No se pudo obtener lock para disparar contingencia automática. Suc={Suc}, PV={PV}",
                        key.suc, key.pv);
                    return;
                }

                // Re-verificar bajo lock (otro thread pudo haberla abierto mientras esperábamos).
                if (estado.ContingenciaActiva)
                    return;

                using var scope = _scopeFactory.CreateScope();
                var eventoSvc = scope.ServiceProvider
                    .GetRequiredService<IEventoSignificativoSiatService>();

                // Re-verificar contra BD por si hay una contingencia abierta que el
                // monitor todavía no conoce (caso arranque o múltiples instancias).
                var estadoActual = await eventoSvc.ObtenerEstadoContingenciaAsync(
                    codigoSucursal, codigoPuntoVenta, ct);

                if (estadoActual.ContingenciaActiva)
                {
                    estado.ContingenciaActiva = true;
                    estado.EventoSignificativoId = estadoActual.EventoSignificativoId;
                    estado.CodigoRecepcionEventoSignificativo = estadoActual.CodigoRecepcionEventoSignificativo;
                    estado.Origen = estadoActual.Origen;
                    _logger.LogInformation(
                        "Contingencia ya estaba activa (BD). Reutilizando. Suc={Suc}, PV={PV}, EventoId={Id}",
                        key.suc, key.pv, estadoActual.EventoSignificativoId);
                    return;
                }

                _logger.LogWarning(
                    "Umbral de fallos SIAT cruzado: {N} fallos consecutivos. "
                  + "Registrando contingencia automática. Suc={Suc}, PV={PV}",
                    estado.FallosConsecutivos, key.suc, key.pv);

                var resultado = await eventoSvc.RegistrarYActivarAsync(
                    motivo: _opts.CurrentValue.MotivoDefault,
                    origen: "Automatico",
                    codigoSucursal: codigoSucursal,
                    codigoPuntoVenta: codigoPuntoVenta,
                    descripcion: _opts.CurrentValue.DescripcionDefault,
                    ct: ct);

                estado.ContingenciaActiva = true;
                estado.EventoSignificativoId = resultado.EventoId;
                estado.CodigoRecepcionEventoSignificativo = resultado.CodigoRecepcionEventoSignificativo;
                estado.Origen = "Automatico";

                _logger.LogInformation(
                    "Contingencia automática registrada. EventoId={Id}, CodigoRecepcion={Cod}, "
                  + "Suc={Suc}, PV={PV}, Motivo={Motivo}",
                    resultado.EventoId, resultado.CodigoRecepcionEventoSignificativo,
                    key.suc, key.pv, _opts.CurrentValue.MotivoDefault);

                // Reclamo retroactivo de ventas del "período gris" (las que
                // fallaron antes de cruzar el umbral y quedaron Pendiente
                // sin EventoSignificativoSiatId). Transacción SEPARADA del
                // INSERT del evento: si esto falla, no hacemos rollback
                // del evento, que ya está commiteado y el circuito está abierto.
                using var scopeVinculacion = _scopeFactory.CreateScope();
                var dbVinculacion = scopeVinculacion.ServiceProvider
                    .GetRequiredService<IUnitWork>();

                try
                {
                    var vinculadas = await dbVinculacion.ventas
                        .VincularVentasPendientesAlEventoAsync(
                            resultado.EventoId,
                            resultado.FechaHoraInicioEvento,
                            codigoSucursal,
                            codigoPuntoVenta,
                            ct);
                    await dbVinculacion.SaveUnitWork();

                    if (vinculadas == 0)
                    {
                        _logger.LogWarning(
                            "Contingencia {Id} registrada pero SIN ventas del período gris que vincular. "
                          + "Posible causa: ninguna venta falló antes de cruzar el umbral, "
                          + "o el umbral ({Umbral}) se cruzó en una sola ráfaga.",
                            resultado.EventoId, _opts.CurrentValue.UmbralFallosConsecutivos);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Vinculadas {N} ventas del período gris al evento {Id} "
                          + "(FechaInicioEvento={FechaInicio}, Suc={Suc}, PV={PV})",
                            vinculadas, resultado.EventoId,
                            resultado.FechaHoraInicioEvento, codigoSucursal, codigoPuntoVenta);
                    }

                    // Reclamo retroactivo de NOTAS de ajuste del "período gris":
                    // mismo patrón que ventas. Si una nota falló antes del cruce
                    // del umbral y quedó Pendiente sin evento, la vinculamos acá
                    // para que ReenvioFacturasContingenciaService la procese al
                    // recuperar SIAT. Sin esto, las notas del período gris quedan
                    // huérfanas para siempre.
                    try
                    {
                        var vinculadasNotas = await dbVinculacion.notasAjuste
                            .VincularNotasPendientesAlEventoAsync(
                                resultado.EventoId,
                                resultado.FechaHoraInicioEvento,
                                codigoSucursal,
                                codigoPuntoVenta,
                                ct);
                        await dbVinculacion.SaveUnitWork();

                        if (vinculadasNotas > 0)
                        {
                            _logger.LogInformation(
                                "Vinculadas {N} notas del período gris al evento {Id}",
                                vinculadasNotas, resultado.EventoId);
                        }
                    }
                    catch (Exception linkNotaEx)
                    {
                        // NO relanzamos — el evento y las ventas ya están vinculadas.
                        _logger.LogError(linkNotaEx,
                            "Error vinculando notas del período gris al evento {Id}. "
                          + "Las notas previas al cruce del umbral quedaron Pendiente sin evento. "
                          + "Reenvío manual requerido.",
                            resultado.EventoId);
                    }
                }
                catch (Exception linkEx)
                {
                    // NO relanzamos — el evento ya está registrado, el circuito está abierto.
                    // Si falla la vinculación, las ventas previas al cruce del umbral
                    // quedaron Pendiente sin evento y requieren reenvío manual.
                    _logger.LogError(linkEx,
                        "Error vinculando ventas del período gris al evento {Id}. "
                      + "Las ventas previas al cruce del umbral quedaron Pendiente sin evento. "
                      + "Reenvío manual requerido.",
                        resultado.EventoId);
                }
            }
            catch (VentaException vex)
            {
                // Caso típico: ya existe contingencia activa (otro thread fue más rápido)
                // o el SIAT rechazó el registro porque volvió a responder.
                // NO hacemos fallback local: VentaException significa config bug o
                // error de negocio del SIAT, no conectividad caída.
                _logger.LogWarning(
                    "No se pudo registrar contingencia automática: {Msg}. Suc={Suc}, PV={PV}",
                    vex.Message, key.suc, key.pv);
            }
            catch (Exception ex)
            {
                // Pieza 2 — fallback local cuando el SIAT está caído.
                // Distinguimos errores de conectividad (SiatOfflineException,
                // HttpRequestException, TaskCanceledException por timeout) de
                // otros errores. Solo los primeros ameritan fallback porque
                // son los que indican "SIAT no responde".
                var esOffline = ex is SiatOfflineException
                    || ex is HttpRequestException
                    || ex is TaskCanceledException;

                if (!esOffline)
                {
                    _logger.LogError(ex,
                        "Error inesperado al registrar contingencia automática (no es offline). Suc={Suc}, PV={PV}",
                        key.suc, key.pv);
                    return;
                }

                _logger.LogWarning(
                    "SIAT no responde al registrar contingencia automática ({ExType}: {ExMsg}). "
                  + "Intentando fallback local. Suc={Suc}, PV={PV}",
                    ex.GetType().Name, ex.Message, key.suc, key.pv);

                // Reusamos el scope creado arriba (línea 244) para no crear uno
                // nuevo innecesariamente. eventoSvc sigue válido aquí.
                try
                {
                    // Creamos un scope nuevo aquí (no podemos reusar el del try
                    // principal porque ya quedó fuera de scope al entrar al catch).
                    using var scopeLocal = _scopeFactory.CreateScope();
                    var eventoSvcLocal = scopeLocal.ServiceProvider
                        .GetRequiredService<IEventoSignificativoSiatService>();

                    var resultadoLocal = await eventoSvcLocal.RegistrarLocalmenteSinSoapAsync(
                        motivo: _opts.CurrentValue.MotivoDefault,
                        origen: "AutomaticoSinSoap",
                        codigoSucursal: codigoSucursal,
                        codigoPuntoVenta: codigoPuntoVenta,
                        descripcion: _opts.CurrentValue.DescripcionDefault,
                        ct: ct);

                    estado.ContingenciaActiva = true;
                    estado.EventoSignificativoId = resultadoLocal.EventoId;
                    estado.CodigoRecepcionEventoSignificativo = null;
                    estado.Origen = "AutomaticoSinSoap";

                    _logger.LogWarning(
                        "Contingencia registrada LOCALMENTE sin SOAP (fallback Pieza 2). "
                      + "EventoId={Id}, Suc={Suc}, PV={PV}, Motivo={Motivo}",
                        resultadoLocal.EventoId, key.suc, key.pv, _opts.CurrentValue.MotivoDefault);

                    // Reclamo retroactivo de ventas del "período gris" — mismo
                    // patrón que el camino feliz, en transacción separada para
                    // no hacer rollback del INSERT del evento si esto falla.
                    using var scopeVinculacion = _scopeFactory.CreateScope();
                    var dbVinculacion = scopeVinculacion.ServiceProvider
                        .GetRequiredService<IUnitWork>();

                    try
                    {
                        var vinculadas = await dbVinculacion.ventas
                            .VincularVentasPendientesAlEventoAsync(
                                resultadoLocal.EventoId,
                                resultadoLocal.FechaHoraInicioEvento,
                                codigoSucursal,
                                codigoPuntoVenta,
                                ct);
                        await dbVinculacion.SaveUnitWork();

                        if (vinculadas == 0)
                        {
                            _logger.LogWarning(
                                "Contingencia local {Id} registrada pero SIN ventas del período gris. "
                              + "Posible causa: ninguna venta falló antes de cruzar el umbral, "
                              + "o el umbral ({Umbral}) se cruzó en una sola ráfaga.",
                                resultadoLocal.EventoId, _opts.CurrentValue.UmbralFallosConsecutivos);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Vinculadas {N} ventas del período gris al evento local {Id}",
                                vinculadas, resultadoLocal.EventoId);
                        }

                        // Mismo reclamo retroactivo para notas del período gris (Pieza 2 fallback).
                        try
                        {
                            var vinculadasNotas = await dbVinculacion.notasAjuste
                                .VincularNotasPendientesAlEventoAsync(
                                    resultadoLocal.EventoId,
                                    resultadoLocal.FechaHoraInicioEvento,
                                    codigoSucursal,
                                    codigoPuntoVenta,
                                    ct);
                            await dbVinculacion.SaveUnitWork();

                            if (vinculadasNotas > 0)
                            {
                                _logger.LogInformation(
                                    "Vinculadas {N} notas del período gris al evento local {Id}",
                                    vinculadasNotas, resultadoLocal.EventoId);
                            }
                        }
                        catch (Exception linkNotaEx)
                        {
                            _logger.LogError(linkNotaEx,
                                "Error vinculando notas del período gris al evento local {Id}. "
                              + "Reenvío manual requerido.",
                                resultadoLocal.EventoId);
                        }
                    }
                    catch (Exception linkEx)
                    {
                        _logger.LogError(linkEx,
                            "Error vinculando ventas del período gris al evento local {Id}. "
                          + "Las ventas previas al cruce del umbral quedaron Pendiente sin evento. "
                          + "Reenvío manual requerido.",
                            resultadoLocal.EventoId);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical(fallbackEx,
                        "FALLBACK de contingencia local también falló. "
                      + "El sistema operará SIN contingencia en BD y los cobros pueden fallar. "
                      + "Operador debe intervenir manualmente. Suc={Suc}, PV={PV}",
                        key.suc, key.pv);
                }
            }
            finally
            {
                if (tomado) sem.Release();
            }
        }

        private async Task CerrarContingenciaAsync(
            int eventoId,
            int codigoSucursal,
            int codigoPuntoVenta,
            string? codigoRecepcion,
            CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var eventoSvc = scope.ServiceProvider
                    .GetRequiredService<IEventoSignificativoSiatService>();

                await eventoSvc.CerrarContingenciaAsync(eventoId, ct);

                _logger.LogInformation(
                    "Recuperación SIAT detectada. Contingencia cerrada. EventoId={Id}, "
                  + "Suc={Suc}, PV={PV}, CodigoRecepcion={Cod}",
                    eventoId, codigoSucursal, codigoPuntoVenta, codigoRecepcion ?? "(null)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al cerrar contingencia {Id} tras recuperación detectada",
                    eventoId);
            }
        }

        private static bool EsOperacionCritica(string operacion) =>
            operacion == SiatOperacion.Cuis
            || operacion == SiatOperacion.Cufd
            || operacion == SiatOperacion.FechaHora
            || operacion == SiatOperacion.RecepcionFactura
            // FIX #1: ValidacionRecepcionPaqueteFactura es parte del path crítico
            // contingencia — sus fallos deben contar para el umbral.
            || operacion == SiatOperacion.ValidacionRecepcionPaqueteFactura;

        /// <summary>Estado interno del monitor por (sucursal, puntoVenta).</summary>
        private sealed class EstadoConexion
        {
            public int FallosConsecutivos { get; set; }
            public DateTime? PrimerFalloUtc { get; set; }
            public bool ContingenciaActiva { get; set; }
            public int? EventoSignificativoId { get; set; }
            public string? CodigoRecepcionEventoSignificativo { get; set; }
            public string? Origen { get; set; }

            /// <summary>
            /// Timestamp del último fallo de Pieza 3b (reenvío del registro del
            /// evento significativo al SIAT). Si está seteado, el monitor NO
            /// reintenta Pieza 3b en próximos ciclos para evitar un bucle
            /// infinito de 60s. Se resetea sólo cuando el operador interviene
            /// manualmente (restart del backend lo limpia porque el estado se
            /// reconstruye desde BD en InicializarAsync).
            /// </summary>
            public DateTime? Pieza3bFalloUtc { get; set; }

            /// <summary>
            /// Lock lógico de re-entrada: true mientras hay una instancia de
            /// <c>ReenviarRegistroAsync</c> en curso para este (suc, pv).
            /// <para>
            /// <c>ReportarExitoAsync</c> chequea esto al inicio y retorna
            /// inmediato si ya hay una en vuelo, evitando recursión desde
            /// los SOAPs exitosos del propio flujo de Pieza 3b
            /// (<c>EnviarSoapAsync</c> llama <c>ReportarExitoAsync</c>
            /// después de cada SOAP crítico exitoso — sin este guard, cada
            /// <c>SolicitarCuisAsync</c>/<c>SolicitarCufdAsync</c>/<c>
            /// RegistroEventoSignificativoAsync</c> dentro del helper
            /// dispara OTRA instancia del helper, generando N llamadas
            /// concurrentes que saturan el threadpool y la BD).
            /// </para>
            /// </summary>
            public bool Pieza3bEnProgreso { get; set; }
        }
    }
}