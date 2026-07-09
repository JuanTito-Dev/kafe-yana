using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.SiatConnectivity
{
    /// <summary>
    /// Lógica de reenvío de facturas y notas de ajuste emitidas en contingencia
    /// (TipoEmision=2) una vez que el SIAT vuelve a responder. Se ejecuta fuera
    /// del flujo HTTP (desde un BackgroundService) pero se expone como Scoped
    /// para poder testearlo sin levantar el host.
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class ReenvioFacturasContingenciaService
    {
        private readonly IUnitWork _db;
        private readonly IFacturaSiatEnvioService _facturaSiatEnvio;
        private readonly INotaAjusteSiatEnvioService _notaSiatEnvio;
        private readonly IRecepcionFacturaService _recepcionFactura;
        private readonly SiatOptions _opts;
        private readonly ILogger<ReenvioFacturasContingenciaService> _logger;
        private readonly IContingenciaDebugLogService _debug;

        public ReenvioFacturasContingenciaService(
            IUnitWork db,
            IFacturaSiatEnvioService facturaSiatEnvio,
            INotaAjusteSiatEnvioService notaSiatEnvio,
            IRecepcionFacturaService recepcionFactura,
            IOptions<SiatOptions> opts,
            ILogger<ReenvioFacturasContingenciaService> logger,
            IContingenciaDebugLogService debug)
        {
            _db = db;
            _facturaSiatEnvio = facturaSiatEnvio;
            _notaSiatEnvio = notaSiatEnvio;
            _recepcionFactura = recepcionFactura;
            _opts = opts.Value;
            _logger = logger;
            _debug = debug;
        }

        /// <summary>
        /// Reenvía todas las ventas pendientes (EstadoSiat=Pendiente) asociadas
        /// a un evento significativo, agrupándolas en paquetes SOAP
        /// (<c>recepcionPaqueteFactura</c>) con la asociación formal al evento
        /// significativo (campo <c>codigoEvento</c> = CodigoRecepcionEventoSignificativo).
        ///
        /// Subdivide por (suc, pv, evento, cufd) para mantener invariantes WSDL
        /// (mismo cufd/cuis por paquete). Pagina en bloques de CantidadMaximaPaquete.
        /// Ver [[kafeyana-contingencia-paquete-siat]].
        /// </summary>
        public async Task<ResultadoReenvioContingenciaDto> ReenviarVentasPendientesAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            // 1. Cargar el evento. Necesitamos CodigoRecepcionEventoSignificativo
            //    para viajar como codigoEvento (xs:long) en cada paquete SOAP.
            var evento = await _db.eventosSignificativosSiat.FindByIdAsync(eventoSignificativoId)
                ?? throw new VentaException(
                    $"ReenviarVentasPendientesAsync: evento {eventoSignificativoId} no existe");

            if (string.IsNullOrWhiteSpace(evento.CodigoRecepcionEventoSignificativo)
                || evento.CodigoRecepcionEventoSignificativo.Trim() == "0")
            {
                // El evento fue rechazado por SIAT (EstadoContingencia=Rechazado),
                // todavía no se registró OK, o el SIAT devolvió "0" como código.
                // En cualquier caso, sin codigoRecepcion válido no podemos paquetizar.
                var ventasSinCodRecep = await _db.ventas.BuscarPendientesPorEventoAsync(eventoSignificativoId, ct);
                _logger.LogWarning(
                    "ReenviarVentasPendientesAsync: evento {Id} con CodigoRecepcionEventoSignificativo inválido ('{Valor}'). "
                  + "Las {Count} ventas Pendientes NO se procesan hasta que el evento se registre OK en el SIAT.",
                    evento.Id, evento.CodigoRecepcionEventoSignificativo ?? "(null)", ventasSinCodRecep.Count);
                return new ResultadoReenvioContingenciaDto
                {
                    EventoSignificativoId = eventoSignificativoId,
                    PendientesEncontradas = 0
                };
            }

            // 2. Traer ventas Pendientes del evento.
            var ventas = await _db.ventas.BuscarPendientesPorEventoAsync(eventoSignificativoId, ct);

            if (ventas.Count == 0)
            {
                _logger.LogInformation(
                    "Reenvío contingencia (eventoId={Id}): no hay ventas pendientes.",
                    eventoSignificativoId);
                return new ResultadoReenvioContingenciaDto
                {
                    EventoSignificativoId = eventoSignificativoId,
                    PendientesEncontradas = 0
                };
            }

            // Split: ventas sin CodigoRecepcion → Path A (envío + validación).
            //        ventas con CodigoRecepcion ya asignado → Path B (sólo revalidar).
            //        Evita duplicar recepcionPaqueteFactura cuando SIAT devolvió 901
            //        sostenido en el ciclo anterior y el HostedService reintenta.
            var ventasNuevas = ventas.Where(v => string.IsNullOrWhiteSpace(v.CodigoRecepcion)).ToList();
            var ventasPendienteVal = ventas.Where(v => !string.IsNullOrWhiteSpace(v.CodigoRecepcion)).ToList();

            _logger.LogInformation(
                "Reenvío contingencia (eventoId={Id}): {N} ventas pendientes "
              + "({Nuevas} nuevas para envío, {Revalidar} ya enviadas para revalidar). "
              + "Paquetizando hasta {Max} por SOAP.",
                eventoSignificativoId, ventas.Count,
                ventasNuevas.Count, ventasPendienteVal.Count,
                _opts.CantidadMaximaPaquete);

            // ════════════════════════════════════════════════════════════════
            // LOG DEBUG: batch-level. Usa un correlation ID de batch (sufijo
            // "-BATCH") para que NO se confunda con el correlation ID por
            // paquete que setea RecepcionFacturaService.EnviarRecepcionPaquete-
            // ContingenciaAsync dentro del loop. Restauramos el ambient al
            // final con try/finally para no contaminar otros flows async.
            // Ver [[kafeyana-contingencia-siat]].
            // ════════════════════════════════════════════════════════════════
            var stopwatchBatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString("N")[..15] + "BATCH";
            var previousCorrelationId = ContingenciaContext.CorrelationId;
            ContingenciaContext.CorrelationId = batchId;

            try
            {
                _debug.LogInfo("ReenvioFacturasContingencia", "inicio_batch",
                    $"eventoId={eventoSignificativoId} codRecepEvento={evento.CodigoRecepcionEventoSignificativo} "
                  + $"pendientes={ventas.Count} maxPorPaquete={_opts.CantidadMaximaPaquete} "
                  + $"estadoContingencia={evento.EstadoContingencia}");
            }
            finally
            {
                ContingenciaContext.CorrelationId = previousCorrelationId;
            }

            int totalValidadas = 0, totalObservadas = 0, totalErrores = 0;
            int totalProcesadas = 0;

            // 3. Subdividir por (suc, pv, evento, cufd). Invariante WSDL: cufd/cuis
            //    uniformes por paquete (todas las facturas del paquete deben compartir
            //    el mismo CUFD). Si un evento cubre facturas con CUFDs distintos
            //    (rollover de día durante contingencia), subdividimos.
            var grupos = ventasNuevas
                .GroupBy(v => new
                {
                    v.CodigoSucursal,
                    v.CodigoPuntoVenta,
                    v.EventoSignificativoSiatId,
                    Cufd = v.Cufd ?? string.Empty
                })
                .ToList();

            int paqueteGlobalIdx = 0;

            foreach (var grupo in grupos)
            {
                if (ct.IsCancellationRequested) break;

                var lote = grupo.OrderBy(v => v.FechaEmision).ToList();

                // LOG DEBUG: planificar grupo (suc, pv, cufd) antes de subdividir
                // en paquetes. Útil para detectar invariantes WSDL violadas.
                try
                {
                    ContingenciaContext.CorrelationId = batchId;
                    _debug.LogInfo("ReenvioFacturasContingencia", "grupo_planificado",
                        $"suc={grupo.Key.CodigoSucursal} pv={grupo.Key.CodigoPuntoVenta} "
                      + $"cufdLen={grupo.Key.Cufd.Length} ventas={lote.Count}");
                }
                finally
                {
                    ContingenciaContext.CorrelationId = previousCorrelationId;
                }

                for (int i = 0; i < lote.Count; i += _opts.CantidadMaximaPaquete)
                {
                    if (ct.IsCancellationRequested) break;

                    var paquete = lote.Skip(i).Take(_opts.CantidadMaximaPaquete).ToList();

                    // LOG DEBUG: cada paquete SOAP planificado (idx global).
                    try
                    {
                        ContingenciaContext.CorrelationId = batchId;
                        _debug.LogInfo("ReenvioFacturasContingencia", "paquete_planificado",
                            $"paqueteIdx={paqueteGlobalIdx} ventas={paquete.Count} "
                          + $"loteIdx={i}-{i + paquete.Count}/{lote.Count} "
                          + $"suc={grupo.Key.CodigoSucursal} pv={grupo.Key.CodigoPuntoVenta}");
                    }
                    finally
                    {
                        ContingenciaContext.CorrelationId = previousCorrelationId;
                    }

                    // Capturar CUFD ANTES del SOAP: Pieza 3b puede haber actualizado
                    // evento.CufdEvento a un CUFD fresco; la validación debe usar el
                    // mismo CUFD que se envió en el sobre recepción (Bug 2).
                    var cufdPaquete = paquete[0].Cufd ?? string.Empty;

                    RespuestaRecepcionPaqueteFacturaDto respuesta;
                    try
                    {
                        // RecepcionFacturaService setea su PROPIO correlation ID por
                        // paquete con _debug.BeginScope(...). Eso se ve claramente
                        // en el log porque cambia de "-BATCH" al ID nuevo de 16 chars.
                        respuesta = await _recepcionFactura.EnviarRecepcionPaqueteContingenciaAsync(
                            paquete, evento, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Reenvío contingencia: excepción SOAP al enviar paquete "
                          + "(eventoId={Id}, lote={From}-{To} de {Total})",
                            evento.Id, i, i + paquete.Count, lote.Count);

                        // LOG DEBUG: paquete falló por excepción.
                        try
                        {
                            ContingenciaContext.CorrelationId = batchId;
                            _debug.LogError("ReenvioFacturasContingencia", "paquete_excepcion",
                                $"paqueteIdx={paqueteGlobalIdx} ventas={paquete.Count} ex={ex.GetType().Name}: {ex.Message}",
                                ex);
                        }
                        finally
                        {
                            ContingenciaContext.CorrelationId = previousCorrelationId;
                        }

                        foreach (var v in paquete)
                        {
                            v.EstadoSiat = FacturaEstado.Pendiente;
                            v.ErrorMensaje = $"Error SOAP paquete: {ex.Message}";
                            totalErrores++;
                        }
                        paqueteGlobalIdx++;
                        continue;
                    }

                    var mapeo = await MapearRespuestaPaqueteAsync(paquete, evento, respuesta, cufdPaquete, ct);
                    totalValidadas += mapeo.Validadas;
                    totalObservadas += mapeo.Observadas;
                    totalErrores += mapeo.Errores;
                    totalProcesadas += paquete.Count;

                    // LOG DEBUG: paquete procesado (resultado agregado).
                    try
                    {
                        ContingenciaContext.CorrelationId = batchId;
                        _debug.LogInfo("ReenvioFacturasContingencia", "paquete_resultado",
                            $"paqueteIdx={paqueteGlobalIdx} ventas={paquete.Count} "
                          + $"transaccion={respuesta.Transaccion} codRecep={respuesta.CodigoRecepcion ?? "(null)"} "
                          + $"validadas={mapeo.Validadas} observadas={mapeo.Observadas} "
                          + $"errores={mapeo.Errores} pendientes={mapeo.Pendientes}");
                    }
                    finally
                    {
                        ContingenciaContext.CorrelationId = previousCorrelationId;
                    }

                    paqueteGlobalIdx++;
                }
            }

            // ════════════════════════════════════════════════════════════════
            // Path B: ventas que ya tienen CodigoRecepcion de un envío previo.
            // No llamar recepcionPaqueteFactura (evita duplicados en piloto/prod).
            // Sólo revalidar con validacionRecepcionPaqueteFactura.
            // ════════════════════════════════════════════════════════════════
            var gruposRevalidacion = ventasPendienteVal
                .GroupBy(v => v.CodigoRecepcion!)
                .ToList();

            foreach (var grupoVal in gruposRevalidacion)
            {
                if (ct.IsCancellationRequested) break;

                var codRecepVal = grupoVal.Key;
                // Mismo orden que el TAR.GZ original para que NumeroArchivo coincida.
                var loteVal = grupoVal.OrderBy(v => v.FechaEmision).ToList();
                var cufdPaqueteVal = loteVal[0].Cufd ?? string.Empty;

                try
                {
                    ContingenciaContext.CorrelationId = batchId;
                    _debug.LogInfo("ReenvioFacturasContingencia", "revalidacion_inicio",
                        $"codRecep={codRecepVal} ventas={loteVal.Count} cufdLen={cufdPaqueteVal.Length}");
                }
                finally { ContingenciaContext.CorrelationId = previousCorrelationId; }

                var ventasRechazadasVal = new HashSet<int>();
                int? estadoVal = null;
                RespuestaValidacionRecepcionPaqueteDto validacionVal = null!;

                for (int intento = 0; intento < 10; intento++)
                {
                    ct.ThrowIfCancellationRequested();

                    validacionVal = await _recepcionFactura.ValidarRecepcionPaqueteContingenciaAsync(
                        codRecepVal,
                        evento.CodigoSucursal,
                        evento.CodigoPuntoVenta,
                        evento.CodigoAmbiente,
                        evento.Cuis ?? string.Empty,
                        cufdPaqueteVal,
                        _opts.CodigoSistema,
                        _opts.Nit,
                        ct);

                    estadoVal = validacionVal.CodigoEstado;

                    if (estadoVal is 908 or 904)
                    {
                        foreach (var msg in validacionVal.MensajesList ?? new())
                        {
                            if (string.IsNullOrWhiteSpace(msg.NumeroArchivo)) continue;
                            if (!int.TryParse(msg.NumeroArchivo, out var num)
                                || num < 1 || num > loteVal.Count) continue;
                            var idx = num - 1;
                            if (ventasRechazadasVal.Contains(idx)) continue;
                            var vr = loteVal[idx];
                            vr.EstadoSiat = FacturaEstado.Observada;
                            vr.ErrorMensaje = $"Observada en revalidación: [{(msg.Codigo ?? "?")}] {msg.Descripcion ?? "(sin descripción)"}";
                            ventasRechazadasVal.Add(idx);
                        }
                        break;
                    }

                    if (estadoVal is 901)
                    {
                        _logger.LogInformation(
                            "Revalidación-only: paquete {CodRecep} en estado 901. Reintento {N}/10.",
                            codRecepVal, intento + 1);
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        continue;
                    }

                    _logger.LogWarning(
                        "Revalidación-only: paquete {CodRecep} estado inesperado {Estado}. Dejando Pendiente.",
                        codRecepVal, estadoVal);
                    break;
                }

                int valVal = 0, obsVal = 0;
                if (estadoVal == 908)
                {
                    foreach (var (idx, v) in loteVal.Select((vv, i) => (i, vv)))
                    {
                        if (ventasRechazadasVal.Contains(idx)) continue;
                        v.EstadoSiat = FacturaEstado.Validada;
                        v.CodigoRecepcion = codRecepVal;
                        v.ErrorMensaje = null;
                        valVal++;
                        totalValidadas++;
                    }
                }
                else if (estadoVal == 904)
                {
                    foreach (var (idx, v) in loteVal.Select((vv, i) => (i, vv)))
                    {
                        if (ventasRechazadasVal.Contains(idx)) continue;
                        v.EstadoSiat = FacturaEstado.Observada;
                        v.ErrorMensaje = "Paquete 904 observada por SIN al revalidar.";
                        obsVal++;
                        totalObservadas++;
                    }
                }
                else
                {
                    // 901 sostenido (>30s) u otro estado desconocido.
                    // Mantener Pendiente — CodigoRecepcion ya está asignado, no tocar.
                    foreach (var (idx, v) in loteVal.Select((vv, i) => (i, vv)))
                    {
                        if (ventasRechazadasVal.Contains(idx)) continue;
                        v.EstadoSiat = FacturaEstado.Pendiente;
                        v.ErrorMensaje = $"Revalidación no convergió tras 10 reintentos (estado {estadoVal?.ToString() ?? "?"}).";
                    }
                }

                try
                {
                    ContingenciaContext.CorrelationId = batchId;
                    _debug.LogInfo("ReenvioFacturasContingencia", "revalidacion_resultado",
                        $"codRecep={codRecepVal} estadoFinal={estadoVal} "
                      + $"validadas={valVal} observadas={obsVal} rechazadas={ventasRechazadasVal.Count}");
                }
                finally { ContingenciaContext.CorrelationId = previousCorrelationId; }

                totalProcesadas += loteVal.Count;
            }

            await _db.SaveUnitWork();

            stopwatchBatch.Stop();

            // LOG DEBUG: fin de batch con totales + duración.
            try
            {
                ContingenciaContext.CorrelationId = batchId;
                _debug.LogInfo("ReenvioFacturasContingencia", "fin_batch",
                    $"eventoId={eventoSignificativoId} procesadas={totalProcesadas} "
                  + $"validadas={totalValidadas} observadas={totalObservadas} errores={totalErrores} "
                  + $"paquetes={paqueteGlobalIdx} duracionMs={stopwatchBatch.ElapsedMilliseconds}");
            }
            finally
            {
                ContingenciaContext.CorrelationId = previousCorrelationId;
            }

            _logger.LogInformation(
                "Reenvío contingencia (eventoId={Id}): procesadas={Procesadas}, "
              + "validadas={Validadas}, observadas={Observadas}, errores={Errores}",
                eventoSignificativoId, totalProcesadas, totalValidadas, totalObservadas, totalErrores);

            return new ResultadoReenvioContingenciaDto
            {
                EventoSignificativoId = eventoSignificativoId,
                PendientesEncontradas = ventas.Count,
                Validadas = totalValidadas,
                Observadas = totalObservadas,
                Errores = totalErrores
            };
        }

        /// <summary>
        /// FIX #1 — mapea la respuesta del paquete a cada Venta, con la lógica asíncrona
        /// nueva:
        ///
        /// 1. Si Transaccion=false → todas Pendiente con ErrorMensaje (igual que antes).
        /// 2. Si Transaccion=true pero codigoRecepcion null → Observadas (igual que antes).
        /// 3. Aplica rechazos granulares de <c>mensajesList</c> por <c>numeroArchivo</c>
        ///    (cada entrada 1..N apunta a una venta del paquete en orden FIFO de
        ///    <c>FechaEmision</c>, mismo orden en que se enviaron al SIAT).
        /// 4. Llama a <c>validacionRecepcionPaqueteFactura</c> y POLLING
        ///    (max 5 reintentos, 1s entre cada) hasta que el SIN devuelva:
        ///    - 908 validada → todas las no-rechazadas quedan Validada con codRecep global.
        ///    - 904 observada → todas las no-rechazadas quedan Observada con el motivo del SIN.
        ///    - 901 pendiente (sin converger tras 5 reintentos) → no cambia estado, deja
        ///      el flag; el próximo intento de ContingencyResendHostedService las reprocesará.
        ///
        /// Antes de este fix: marcaba Validadas apenas el SOAP síncrono respondía
        /// transaccion=true (estado provisional), descartando mensajesList.
        /// </summary>
        private async Task<(int Validadas, int Observadas, int Errores, int Pendientes)>
            MapearRespuestaPaqueteAsync(
                IReadOnlyList<Venta> paquete,
                EventoSignificativoSiat evento,
                RespuestaRecepcionPaqueteFacturaDto respuesta,
                string cufdPaquete,
                CancellationToken ct)
        {
            int validadas = 0, observadas = 0, errores = 0, pendientes = 0;

            // 1. SOAP síncrono rechazó el paquete entero.
            if (!respuesta.Transaccion)
            {
                var mensajes = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                foreach (var v in paquete)
                {
                    v.EstadoSiat = FacturaEstado.Pendiente;
                    v.ErrorMensaje = $"Paquete rechazado por SIAT: {mensajes}";
                    errores++;
                }
                return (0, 0, errores, 0);
            }

            if (string.IsNullOrWhiteSpace(respuesta.CodigoRecepcion))
            {
                foreach (var v in paquete)
                {
                    v.EstadoSiat = FacturaEstado.Observada;
                    v.ErrorMensaje = "Paquete aceptado sin codigoRecepcion (shape de respuesta inesperado)";
                    observadas++;
                }
                return (0, observadas, 0, 0);
            }

            var codRecep = respuesta.CodigoRecepcion!;

            // 2. Aplicar rechazos granulares de mensajesList/numeroArchivo.
            //    numeroArchivo es 1..N en el orden en que las ventas viajaron en el TAR.GZ,
            //    que es el orden FIFO por FechaEmision que ArmarArchivoPaquete respeta.
            var rechazosPorArchivo = (respuesta.MensajesList ?? new())
                .Where(m => !string.IsNullOrWhiteSpace(m.NumeroArchivo)
                         && string.Equals(m.Advertencia, "true", StringComparison.OrdinalIgnoreCase) == false)
                .ToDictionary(m => m.NumeroArchivo!, m => m);

            // Conjunto de posiciones del paquete (1-indexed) que el SIN rechazó.
            var ventasRechazadas = new HashSet<int>();
            for (int idx = 0; idx < paquete.Count; idx++)
            {
                var numArchivo = (idx + 1).ToString();
                if (rechazosPorArchivo.TryGetValue(numArchivo, out var msg))
                {
                    var v = paquete[idx];
                    v.EstadoSiat = FacturaEstado.Observada;
                    v.ErrorMensaje = $"Rechazada por SIN: [{(msg.Codigo ?? "?")}] {msg.Descripcion ?? "(sin descripción)"}";
                    ventasRechazadas.Add(idx);
                    _logger.LogWarning(
                        "FIX #1: Venta {VentaId} rechazada por SIN en paquete (archivo {N}): [{Cod}] {Desc}",
                        v.Id, numArchivo, msg.Codigo, msg.Descripcion);
                }
            }

            // 3. Polling validacionRecepcionPaqueteFactura hasta estado definitivo.
            //    901 transitorio (paquete recibido pero todavía procesado) requiere espera.
            //    908 = validada → marcamos las no-rechazadas como Validadas.
            //    904 = observada → marcamos como Observadas con la descripción del SIN.
            //    Timeout (10 intentos × 3s = 30s) → no marcamos, dejamos Pendiente para reintento
            //    posterior desde ContingencyResendHostedService.
            //    Usamos los datos del evento (que es invariante para todas las ventas
            //    del paquete) y NO de `muestra` (que es una venta individual y no
            //    carga CodigoAmbiente ni Cuis en el modelo Venta).
            RespuestaValidacionRecepcionPaqueteDto validacion;
            int? estadoDefinitivo = null;
            for (int intento = 0; intento < 10; intento++)
            {
                ct.ThrowIfCancellationRequested();

                validacion = await _recepcionFactura.ValidarRecepcionPaqueteContingenciaAsync(
                    codRecep,
                    evento.CodigoSucursal,
                    evento.CodigoPuntoVenta,
                    evento.CodigoAmbiente,
                    evento.Cuis ?? string.Empty,
                    cufdPaquete,                 // FIX #11: CUFD real del sobre enviado (no evento.CufdEvento que Pieza3b ya mutó)
                    _opts.CodigoSistema,
                    _opts.Nit,
                    ct);

                estadoDefinitivo = validacion.CodigoEstado;

                if (estadoDefinitivo is 908 or 904)
                {
                    // Procesar también rechazos granulares de la validación.
                    foreach (var msg in validacion.MensajesList ?? new())
                    {
                        if (string.IsNullOrWhiteSpace(msg.NumeroArchivo)) continue;
                        if (!int.TryParse(msg.NumeroArchivo, out var num) || num < 1 || num > paquete.Count) continue;
                        var idx = num - 1;
                        if (ventasRechazadas.Contains(idx)) continue;
                        var v = paquete[idx];
                        v.EstadoSiat = FacturaEstado.Observada;
                        v.ErrorMensaje = $"Observada en validación: [{(msg.Codigo ?? "?")}] {msg.Descripcion ?? "(sin descripción)"}";
                        ventasRechazadas.Add(idx);
                    }
                    break;
                }

                if (estadoDefinitivo is 901)
                {
                    _logger.LogInformation(
                        "FIX #1: paquete {CodRecep} en estado 901 (pendiente). Reintento {N}/10.",
                        codRecep, intento + 1);
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                    continue;
                }

                // Estado desconocido (no 901/904/908) → tratar como pendiente, sin marcar.
                _logger.LogWarning(
                    "FIX #1: paquete {CodRecep} devolvió estado inesperado {Estado}. "
                  + "Dejando ventas Pendientes para reintento.",
                    codRecep, estadoDefinitivo);
                break;
            }

            if (estadoDefinitivo == 908)
            {
                foreach (var (idx, v) in paquete.Select((vv, i) => (i, vv)))
                {
                    if (ventasRechazadas.Contains(idx)) continue;
                    v.EstadoSiat = FacturaEstado.Validada;
                    v.CodigoRecepcion = codRecep;
                    v.ErrorMensaje = null;
                    validadas++;
                }
            }
            else if (estadoDefinitivo == 904)
            {
                foreach (var (idx, v) in paquete.Select((vv, i) => (i, vv)))
                {
                    if (ventasRechazadas.Contains(idx)) continue;
                    v.EstadoSiat = FacturaEstado.Observada;
                    v.ErrorMensaje = "Paquete 904 observada por SIN al validar.";
                    observadas++;
                }
            }
            else
            {
                // 901 sostenido u otro estado — no marcar.
                foreach (var (idx, v) in paquete.Select((vv, i) => (i, vv)))
                {
                    if (ventasRechazadas.Contains(idx)) continue;
                    v.EstadoSiat = FacturaEstado.Pendiente;
                    v.CodigoRecepcion = codRecep;
                    v.ErrorMensaje = $"Paquete transaccion=true pero validación no convergió (estado {estadoDefinitivo?.ToString() ?? "?"}). Reintento posterior.";
                    pendientes++;
                }
            }

            _logger.LogInformation(
                "FIX #1 MapearRespuestaPaquete: codRecep={CodRecep}, estadoFinal={Estado}, "
              + "validadas={Validadas}, observadas={Observadas}, pendientes={Pendientes}, errores={Errores}",
                codRecep, estadoDefinitivo, validadas, observadas, pendientes, errores);

            return (validadas, observadas, errores, pendientes);
        }

        /// <summary>
        /// Reenvía todas las notas de ajuste pendientes (EstadoSiat=Pendiente)
        /// asociadas a un evento significativo. Espejo de
        /// <see cref="ReenviarVentasPendientesAsync"/> — misma lógica, distinto
        /// servicio. Devuelve el mismo <see cref="ResultadoReenvioContingenciaDto"/>
        /// para que el caller agregue ambas métricas si lo necesita.
        /// </summary>
        public async Task<ResultadoReenvioContingenciaDto> ReenviarNotasPendientesAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            var notas = await _db.notasAjuste.BuscarPendientesPorEventoAsync(eventoSignificativoId, ct);

            if (notas.Count == 0)
            {
                _logger.LogInformation(
                    "Reenvío contingencia notas (eventoId={Id}): no hay notas pendientes.",
                    eventoSignificativoId);
                return new ResultadoReenvioContingenciaDto
                {
                    EventoSignificativoId = eventoSignificativoId,
                    PendientesEncontradas = 0
                };
            }

            _logger.LogInformation(
                "Reenvío contingencia notas (eventoId={Id}): {N} notas pendientes. Procesando en orden FIFO por FechaEmision.",
                eventoSignificativoId, notas.Count);

            // LOG DEBUG: batch-level con correlation ID separado para notas
            // (sufijo "-NOTAS") para no mezclarse con el de facturas.
            var stopwatchBatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString("N")[..14] + "NOTAS";
            var previousCorrelationId = ContingenciaContext.CorrelationId;
            ContingenciaContext.CorrelationId = batchId;

            try
            {
                _debug.LogInfo("ReenvioFacturasContingencia", "inicio_batch_notas",
                    $"eventoId={eventoSignificativoId} notas={notas.Count}");
            }
            finally
            {
                ContingenciaContext.CorrelationId = previousCorrelationId;
            }

            int validadas = 0, observadas = 0, errores = 0;

            foreach (var nota in notas)
            {
                if (ct.IsCancellationRequested) break;

                if (nota.EventoSignificativoSiat is null)
                {
                    _logger.LogError(
                        "Nota {NotaId} asociada a evento {Id} pero sin nav prop cargada. Saltando.",
                        nota.Id, eventoSignificativoId);

                    try
                    {
                        ContingenciaContext.CorrelationId = batchId;
                        _debug.LogWarn("ReenvioFacturasContingencia", "nota_sin_evento",
                            $"notaId={nota.Id} numeroNota={nota.NumeroNotaCreditoDebito}", null);
                    }
                    finally
                    {
                        ContingenciaContext.CorrelationId = previousCorrelationId;
                    }

                    errores++;
                    continue;
                }

                try
                {
                    var resultado = await _notaSiatEnvio.ReenviarNotaAsync(nota.Id, ct);

                    if (resultado.Transaccion && resultado.ErrorMensaje is null)
                    {
                        validadas++;
                        try
                        {
                            ContingenciaContext.CorrelationId = batchId;
                            _debug.LogInfo("ReenvioFacturasContingencia", "nota_validada",
                                $"notaId={nota.Id} numeroNota={nota.NumeroNotaCreditoDebito}");
                        }
                        finally
                        {
                            ContingenciaContext.CorrelationId = previousCorrelationId;
                        }
                    }
                    else if (resultado.Enviado)
                    {
                        // Enviado=true + Transaccion=false → quedó diferida de nuevo
                        // (probablemente volvió a tirar SiatOfflineException). No
                        // la contamos como observada — sólo como diferida.
                        _logger.LogInformation(
                            "Reenvío contingencia notas: NotaId={NotaId} (NumeroNota={NF}) "
                          + "volvió a quedar diferida. Mensaje={Msg}",
                            nota.Id, nota.NumeroNotaCreditoDebito, resultado.ErrorMensaje);
                        try
                        {
                            ContingenciaContext.CorrelationId = batchId;
                            _debug.LogWarn("ReenvioFacturasContingencia", "nota_diferida",
                                $"notaId={nota.Id} numeroNota={nota.NumeroNotaCreditoDebito} msg={resultado.ErrorMensaje}", null);
                        }
                        finally
                        {
                            ContingenciaContext.CorrelationId = previousCorrelationId;
                        }
                    }
                    else
                    {
                        observadas++;
                        _logger.LogWarning(
                            "Reenvío contingencia notas: NotaId={NotaId} (NumeroNota={NF}) "
                          + "NO validada por SIAT. {Error}",
                            nota.Id, nota.NumeroNotaCreditoDebito, resultado.ErrorMensaje);
                        try
                        {
                            ContingenciaContext.CorrelationId = batchId;
                            _debug.LogWarn("ReenvioFacturasContingencia", "nota_observada",
                                $"notaId={nota.Id} numeroNota={nota.NumeroNotaCreditoDebito} error={resultado.ErrorMensaje}", null);
                        }
                        finally
                        {
                            ContingenciaContext.CorrelationId = previousCorrelationId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex,
                        "Reenvío contingencia notas: excepción al enviar NotaId={NotaId} (NumeroNota={NF})",
                        nota.Id, nota.NumeroNotaCreditoDebito);
                    try
                    {
                        ContingenciaContext.CorrelationId = batchId;
                        _debug.LogError("ReenvioFacturasContingencia", "nota_excepcion",
                            $"notaId={nota.Id} numeroNota={nota.NumeroNotaCreditoDebito}", ex);
                    }
                    finally
                    {
                        ContingenciaContext.CorrelationId = previousCorrelationId;
                    }
                }
            }

            await _db.SaveUnitWork();

            stopwatchBatch.Stop();
            try
            {
                ContingenciaContext.CorrelationId = batchId;
                _debug.LogInfo("ReenvioFacturasContingencia", "fin_batch_notas",
                    $"eventoId={eventoSignificativoId} total={notas.Count} "
                  + $"validadas={validadas} observadas={observadas} errores={errores} "
                  + $"duracionMs={stopwatchBatch.ElapsedMilliseconds}");
            }
            finally
            {
                ContingenciaContext.CorrelationId = previousCorrelationId;
            }

            _logger.LogInformation(
                "Reenvío contingencia notas (eventoId={Id}): total={Total}, validadas={Validadas}, observadas={Observadas}, errores={Errores}",
                eventoSignificativoId, notas.Count, validadas, observadas, errores);

            return new ResultadoReenvioContingenciaDto
            {
                EventoSignificativoId = eventoSignificativoId,
                PendientesEncontradas = notas.Count,
                Validadas = validadas,
                Observadas = observadas,
                Errores = errores
            };
        }
    }

    public class ResultadoReenvioContingenciaDto
    {
        public int EventoSignificativoId { get; set; }
        public int PendientesEncontradas { get; set; }
        public int Validadas { get; set; }
        public int Observadas { get; set; }
        public int Errores { get; set; }
    }
}