using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Orquestador del flujo de emisión de una nota de ajuste:
    /// 1) Carga la Venta original (debe estar Validada).
    /// 2) Construye NotaAjuste + NotaAjusteDetalle desde el DTO.
    /// 3) Llama al preparer (que valida, calcula, gzipea, hashea).
    /// 4) Envía al SIAT.
    /// 5) Persiste el resultado. La Venta original NO se modifica.
    /// </summary>
    public class NotaAjusteSiatEnvioService : INotaAjusteSiatEnvioService
    {
        private readonly IUnitWork _db;
        private readonly INotaAjusteSiatPreparer _preparer;
        private readonly IRecepcionNotaAjusteService _recepcionNota;
        private readonly ISiatConnectivityMonitor _monitor;
        private readonly ILogger<NotaAjusteSiatEnvioService> _logger;

        public NotaAjusteSiatEnvioService(
            IUnitWork db,
            INotaAjusteSiatPreparer preparer,
            IRecepcionNotaAjusteService recepcionNota,
            ISiatConnectivityMonitor monitor,
            ILogger<NotaAjusteSiatEnvioService> logger)
        {
            _db = db;
            _preparer = preparer;
            _recepcionNota = recepcionNota;
            _monitor = monitor;
            _logger = logger;
        }

        public async Task<ResultadoEnvioNotaAjusteSiatDto> EmitirYEnviarNotaAsync(
            int ventaId,
            DtoCrearNotaAjuste dto,
            CancellationToken ct = default)
        {
            if (dto is null)
                throw new VentaException("El cuerpo de la solicitud es requerido.");

            if (dto.Detalles is null || dto.Detalles.Count == 0)
                throw new VentaException("Debe enviar al menos un detalle en la nota.");

            var venta = await _db.ventas.TraerVentaConDetallesAsync(ventaId);
            if (venta is null)
                throw new VentaException($"Venta {ventaId} no encontrada.");

            if (!venta.Facturado || venta.EstadoSiat != FacturaEstado.Validada)
                throw new VentaException(
                    "Solo se puede emitir una nota sobre una venta con estado SIAT = Validada (908).");

            if (venta.NumeroFactura is null)
                throw new VentaException("La venta no tiene numeroFactura asignado.");

            // Validar que cada IdDetallePagoOriginal pertenece a la venta y resolver la línea original.
            // Además construimos un map Id → posición (1, 2, 3, ...) dentro de la venta original
            // para asignar NumeroLineaOriginal correctamente (regla SIAT 1049: el detalle de la nota
            // debe referenciar la línea exacta de la factura original).
            var detallesPorId = venta.Detalles.ToDictionary(d => d.Id);
            var posicionPorId = venta.Detalles
                .Select((d, i) => (d.Id, Posicion: i + 1))
                .ToDictionary(x => x.Id, x => x.Posicion);

            // Suma de cantidades ya devueltas por línea en notas VÁLIDAS previas.
            // Permite rechazar una nueva nota que intente devolver más unidades
            // de las que aún quedan disponibles (no contra la cantidad ORIGINAL,
            // sino contra lo efectivamente disponible después de devoluciones previas).
            var devueltasMap = await _db.notasAjuste
                .ObtenerCantidadDevueltaPorDetallePagoAsync(ventaId);

            foreach (var item in dto.Detalles)
            {
                if (!detallesPorId.TryGetValue(item.IdDetallePagoOriginal, out var original))
                    throw new VentaException(
                        $"DetallePago {item.IdDetallePagoOriginal} no pertenece a la venta {ventaId}.");

                var yaDevuelto = devueltasMap.GetValueOrDefault(item.IdDetallePagoOriginal, 0m);
                var cantidadDisponible = original.Cantidad - yaDevuelto;

                if (cantidadDisponible <= 0m)
                    throw new VentaException(
                        $"El producto '{original.Descripcion}' ya fue devuelto en su totalidad "
                        + $"({original.Cantidad} unidades). No queda saldo para devolver.");

                if (item.Cantidad > cantidadDisponible)
                    throw new VentaException(
                        $"Cantidad a devolver ({item.Cantidad}) del producto '{original.Descripcion}' "
                        + $"excede la cantidad disponible ({cantidadDisponible:0.##} = "
                        + $"{original.Cantidad} facturadas − {yaDevuelto} ya devueltas).");
            }

            // Validación de saldo monetario: la suma de los subtotales de esta nota
            // no puede superar el saldo restante (venta.MontoTotal − Σ notas válidas).
            // Alineado con la regla del frontend (sales.mapper.ts:85-103).
            var devueltoPrevio = (await _db.notasAjuste.ListarPorVentaAsync(ventaId))
                .Where(n => n.EstadoSiat == FacturaEstado.Validada)
                .Sum(n => n.MontoTotalDevuelto);

            var montoEstaNota = dto.Detalles
                .Sum(d => Math.Round(
                    d.Cantidad * detallesPorId[d.IdDetallePagoOriginal].PrecioUnitario,
                    2, MidpointRounding.AwayFromZero));

            var saldoDisponible = Math.Max(0m, venta.MontoTotal - devueltoPrevio);
            if (montoEstaNota - 0.01m > saldoDisponible)
                throw new VentaException(
                    $"El monto a devolver ({montoEstaNota:0.00}) excede el saldo disponible "
                    + $"({saldoDisponible:0.00} = {venta.MontoTotal:0.00} venta − "
                    + $"{devueltoPrevio:0.00} ya devuelto).");

            // Calcular totales según reglas SIAT Bolivia (notaComputarizadaCreditoDebito.xsd)
            // Ver [[kafeyana-notaajuste-siat-reglas]] para el detalle de los códigos 1029/1030/1031/1049.
            //
            // Reglas clave (validadas contra rechazos reales del SIAT):
            //   • montoTotalOriginal = suma de subTotal de líneas con codigoDetalleTransaccion=1
            //     (referencias a los items seleccionados). NO es venta.MontoTotal: cuando el
            //     cajero devuelve sólo algunos items, el SIAT espera el subtotal de lo
            //     seleccionado, no el total de la factura. Validado contra error 1030:
            //     "Monto original esperado 22.00 enviado 42.00" cuando venta.MontoTotal=42
            //     y la nota cubre 22.
            //   • montoTotalDevuelto = suma de subTotal de líneas con codigoDetalleTransaccion=2
            //     (NO la suma de TODOS los subtotales). El SIAT computa este valor a partir
            //     de los detalles y rechaza con 1029 si no coincide.
            //   • montoEfectivoCreditoDebito = (montoTotalDevuelto - descuento) * 0.13
            //     (crédito fiscal / IVA, NO el total devuelto). Regla 1031.
            //   • Estructura: por cada producto seleccionado, la UI envía UN detalle con
            //     codigoDetalleTransaccion=1 (marcador semántico). El backend expande
            //     cada uno en un PAR canónico SIAT:
            //       trans=1 → referencia al item original (cantidad=1, subTotal=precioUnitario)
            //       trans=2 → devolución efectiva (cantidad=cantidad devuelta, subTotal=cant*precio)
            //     XSD exige minOccurs=2 en <detalle>.
            var descuento = dto.MontoDescuentoCreditoDebito ?? 0m;

            // El frontend DEBE enviar codigoDetalleTransaccion=1 (marcador semántico por producto).
            // Si envía 2 u otro valor, rechazar — el par lo genera este servicio.
            foreach (var item in dto.Detalles)
            {
                if (item.CodigoDetalleTransaccion != 1)
                    throw new VentaException(
                        $"CodigoDetalleTransaccion debe ser 1 (Devolución) en el body. "
                        + $"Recibido: {item.CodigoDetalleTransaccion} para IdDetallePagoOriginal "
                        + $"{item.IdDetallePagoOriginal}. El par trans=2 lo genera el backend.");
            }

            var detallesExpandidos = ExpandirParesTransaccion(dto.Detalles, detallesPorId, posicionPorId);
            var sumaTrans1 = detallesExpandidos
                .Where(d => d.CodigoDetalleTransaccion == 1)
                .Sum(d => d.SubTotal);
            var sumaTrans2 = detallesExpandidos
                .Where(d => d.CodigoDetalleTransaccion == 2)
                .Sum(d => d.SubTotal);

            var nota = new NotaAjuste
            {
                // Copia cabecera SIAT desde la Venta
                NitEmisor = venta.NitEmisor,
                RazonSocialEmisor = venta.RazonSocialEmisor,
                Municipio = venta.Municipio,
                Telefono = venta.Telefono,
                CodigoSucursal = venta.CodigoSucursal,
                Direccion = venta.Direccion,
                CodigoPuntoVenta = venta.CodigoPuntoVenta,
                CodigoTipoDocumentoIdentidad = venta.CodigoTipoDocumentoIdentidad,
                NumeroDocumento = venta.NumeroDocumento,
                NombreRazonSocial = venta.NombreRazonSocial,
                Complemento = venta.Complemento,
                CodigoCliente = venta.CodigoCliente,
                CodigoExcepcion = venta.CodigoExcepcion,

                // Referencia a la factura original
                IdVenta = venta.Id,
                NumeroFacturaOriginal = venta.NumeroFactura.Value,
                NumeroAutorizacionCuf = venta.Cuf,
                FechaEmisionFactura = venta.FechaEmision,

                // Montos según reglas SIAT (ver comentario de bloque arriba)
                MontoTotalOriginal = sumaTrans1,
                MontoTotalDevuelto = sumaTrans2,
                MontoDescuentoCreditoDebito = descuento,
                MontoEfectivoCreditoDebito = Math.Round((sumaTrans2 - descuento) * 0.13m, 2, MidpointRounding.AwayFromZero),

                // DescuentoAdicional: obligatorio para sector 47 (NCDDE), ignorado
                // para sector 24. Si el DTO no lo manda, default 0 — el generator
                // lo serializa solo cuando sector==47.
                DescuentoAdicional = dto.DescuentoAdicional ?? 0m,

                // Catálogo
                CodigoMotivoAjuste = dto.CodigoMotivoAjuste,

                // Generado luego por el preparer
                NumeroNotaCreditoDebito = await _db.notasAjuste.SiguienteNumeroNotaCreditoDebitoAsync(),

                // Placeholders hasta que el preparer los asigne
                Cuf = "PENDIENTE",
                Cufd = "PENDIENTE",
                Leyenda = string.Empty,
                Usuario = string.IsNullOrWhiteSpace(dto.Usuario) ? "SISTEMA" : dto.Usuario,

                Detalles = detallesExpandidos
            };

            // ─── Preparar ANTES de persistir ─────────────────────────────────
            // El preparer calcula Cuf/Cufd/XmlBase64/CodigoHash. Si lo hiciéramos
            // DESPUÉS de persistir, dejaríamos la nota en BD con Cuf="PENDIENTE"
            // (placeholder) hasta que el preparer corriera; cualquier reintento
            // fallaría con 23505 (IX_NotaAjuste_Cuf) porque ya existiría una fila
            // con ese placeholder único. Por eso: preparar primero, persistir
            // después. Si el preparer lanza, la nota nunca se inserta y no queda
            // basura en BD.
            await _preparer.PrepararNotaAsync(nota, ct);

            // ─── Contingencia: detección temprana ───────────────────────────
            // Si el monitor ya detecta que el SIAT está caído, NO intentamos
            // enviar la nota en línea — la dejamos diferida con TipoEmision=2
            // vinculada al evento activo. Esto es CRÍTICO: el preparer usa
            // TipoEmision para armar el CUF, y un CUF con TipoEmision=1
            // NO se acepta cuando se presenta en contingencia (el SIAT lo
            // rechaza porque la codificación del CUF y el sobre SOAP no
            // coinciden). Mejor diferir la nota desde el origen a tener
            // que "convertirla" después (lo cual implicaría regenerar el
            // CUF, el XML y romper los índices IX_NotaAjuste_Cuf).
            //
            // Decidir ANTES de persistir para que la fila entre en BD ya
            // con TipoEmision=2, EstadoSiat=Pendiente y EventoSignificativoSiatId
            // poblado. Ver [[kafeyana-contingencia-siat]].
            if (!_monitor.EstaEnLinea(venta.CodigoSucursal, venta.CodigoPuntoVenta))
            {
                nota.TipoEmision = 2;
                nota.EventoSignificativoSiatId = _monitor.ObtenerEventoActivo(
                    venta.CodigoSucursal, venta.CodigoPuntoVenta);
                nota.EstadoSiat = FacturaEstado.Pendiente;

                await _db.notasAjuste.Crear(nota);
                await _db.SaveUnitWork();

                _logger.LogInformation(
                    "Nota {Numero} (NotaId={NotaId}) diferida a contingencia upfront. "
                  + "EventoId={EventoId}, Suc={Suc}, PV={PV}",
                    nota.NumeroNotaCreditoDebito, nota.Id, nota.EventoSignificativoSiatId,
                    venta.CodigoSucursal, venta.CodigoPuntoVenta);

                return new ResultadoEnvioNotaAjusteSiatDto
                {
                    Enviado = true,
                    Transaccion = false,        // el SIAT todavía no la validó
                    NotaAjusteId = nota.Id,
                    NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                    Cuf = nota.Cuf,
                    ErrorMensaje = null
                };
            }

            // Persistir con todos los datos finales: Cuf real, XmlBase64,
            // CodigoHash, EstadoSiat=Pendiente. Si el envío al SIAT falla después,
            // la nota queda persistida con esos datos y se puede reintentar vía
            // ReenviarNotaAsync.
            await _db.notasAjuste.Crear(nota);
            await _db.SaveUnitWork();

            // Enviar al SIAT
            return await EnviarAsync(nota, ct);
        }

        public async Task<ResultadoEnvioNotaAjusteSiatDto> ReenviarNotaAsync(
            int notaAjusteId,
            CancellationToken ct = default)
        {
            var nota = await _db.notasAjuste.TraerNotaAjusteConDetallesAsync(notaAjusteId);
            if (nota is null)
                throw new VentaException($"NotaAjuste {notaAjusteId} no encontrada.");

            if (nota.EstadoSiat == FacturaEstado.Validada)
                return new ResultadoEnvioNotaAjusteSiatDto
                {
                    Enviado = false,
                    Transaccion = true,
                    NotaAjusteId = nota.Id,
                    NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                    Cuf = nota.Cuf,
                    CodigoRecepcion = nota.CodigoRecepcion,
                    CodigoDescripcion = "La nota ya está validada por el SIAT.",
                    ErrorMensaje = null
                };

            if (string.IsNullOrWhiteSpace(nota.XmlBase64) || string.IsNullOrWhiteSpace(nota.CodigoHash))
                throw new VentaException(
                    "La nota no tiene XmlBase64/CodigoHash guardados; regenérela.");

            return await EnviarAsync(nota, ct);
        }

        private async Task<ResultadoEnvioNotaAjusteSiatDto> EnviarAsync(NotaAjuste nota, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(nota.XmlBase64))
            {
                nota.EstadoSiat = FacturaEstado.Pendiente;
                nota.ErrorMensaje = "La nota no tiene archivo (XmlBase64) para enviar al SIAT.";
                await _db.SaveUnitWork();

                return new ResultadoEnvioNotaAjusteSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    NotaAjusteId = nota.Id,
                    NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                    ErrorMensaje = nota.ErrorMensaje
                };
            }

            try
            {
                var hash = string.IsNullOrWhiteSpace(nota.CodigoHash) ? null : nota.CodigoHash;

                // Pasamos nota.CodigoSucursal + CodigoPuntoVenta para que el sobre SOAP
                // use EXACTAMENTE el mismo PV con el que se construyó el CUF embebido
                // en nota.Cuf. Si no, el receptor hace una llamada independiente a
                // ObtenerCuisVigenteAsync con appsettings y el SIAT rechaza con
                // 1002 (CUF inválido) + 1008 (PV inválido). Ver mismo patrón en
                // FacturaSiatEnvioService.EnviarVentaAsync (líneas 53-55).
                _logger.LogInformation(
                    "Enviando nota {Numero} (NotaId={NotaId}). Cuf={Cuf}, Suc={Suc}, PV={PV}, TipoEmision={TipoEmision}",
                    nota.NumeroNotaCreditoDebito, nota.Id, nota.Cuf,
                    nota.CodigoSucursal, nota.CodigoPuntoVenta, nota.TipoEmision);

                RespuestaRecepcionNotaAjusteDto respuesta;

                // Ramas:
                // - TipoEmision=2 + EventoSignificativoSiatId poblado → reenvío contingencia.
                //   El sobre SOAP debe llevar codigoRecepcionEventoSignificativo; el CUIS
                //   puede estar vencido, lo cacheamos.
                // - Cualquier otro caso → flujo online normal.
                if (nota.TipoEmision == 2 && nota.EventoSignificativoSiatId is not null)
                {
                    var codigoRecepcionEvento = nota.EventoSignificativoSiat?.CodigoRecepcionEventoSignificativo;

                    if (string.IsNullOrWhiteSpace(codigoRecepcionEvento))
                    {
                        // Fail-closed: si llegamos aquí sin el código de recepción del evento,
                        // no dejamos pasar al SIAT sin ese código porque rechazaría igual.
                        _logger.LogError(
                            "Nota contingencia (NotaId={NotaId}) sin codigoRecepcionEventoSignificativo en EventoSignificativoSiat. "
                          + "Reenvío abortado — corregir el evento en BD antes de reintentar.",
                            nota.Id);

                        nota.EstadoSiat = FacturaEstado.Pendiente;
                        nota.ErrorMensaje = "Falta codigoRecepcionEventoSignificativo en el evento asociado.";
                        await _db.SaveUnitWork();

                        return MapearResultado(nota, default!, enviado: false);
                    }

                    _logger.LogInformation(
                        "Reenviando nota contingencia {Numero} (NotaId={NotaId}, EventoId={EventoId}, CodigoRecepcionEvento={CodRecEv})",
                        nota.NumeroNotaCreditoDebito, nota.Id,
                        nota.EventoSignificativoSiatId, codigoRecepcionEvento);

                    respuesta = await _recepcionNota.EnviarRecepcionContingenciaAsync(
                        nota.XmlBase64, hash,
                        nota.CodigoSucursal, nota.CodigoPuntoVenta,
                        codigoRecepcionEvento, ct);
                }
                else
                {
                    respuesta = await _recepcionNota.EnviarRecepcionAsync(
                        nota.XmlBase64, hash, nota.FechaEmision,
                        nota.CodigoSucursal, nota.CodigoPuntoVenta, ct);
                }

                AplicarResultadoSiat(nota, respuesta);

                await _db.SaveUnitWork();

                if (!respuesta.Transaccion)
                {
                    // Log nivel Error del XML interno decodificado para que el operador
                    // pueda compararlo contra la factura original y diagnosticar el 1049.
                    _logger.LogWarning(
                        "SIAT rechazó nota {Numero} (NotaId={NotaId}). {Error}. " +
                        "CufOriginal={CufOriginal} NumeroFactura={NumeroFactura}",
                        nota.NumeroNotaCreditoDebito, nota.Id, nota.ErrorMensaje,
                        nota.NumeroAutorizacionCuf, nota.NumeroFacturaOriginal);
                    _logger.LogError(
                        "XML enviado al SIAT (base64+gzip decodificado) — NotaId={NotaId}:\n{Xml}",
                        nota.Id,
                        SiatGzip.DescomprimirBase64(nota.XmlBase64));
                }
                else
                {
                    _logger.LogInformation(
                        "Nota {Numero} (NotaId={NotaId}) validada por SIAT. CodigoRecepcion={Codigo}",
                        nota.NumeroNotaCreditoDebito, nota.Id, nota.CodigoRecepcion);
                }

                return MapearResultado(nota, respuesta);
            }
            catch (SiatOfflineException ex)
            {
                // Circuito abierto: NO es un error, es una operación diferida.
                // La nota queda persistida con formato contingencia (TipoEmision=2)
                // y vinculada al evento significativo activo, para que
                // ReenvioFacturasContingenciaService la procese cuando el SIAT vuelva.
                nota.TipoEmision = 2;
                nota.EventoSignificativoSiatId = _monitor.ObtenerEventoActivo(
                    ex.CodigoSucursal, ex.CodigoPuntoVenta);
                nota.EstadoSiat = FacturaEstado.Pendiente;
                nota.ErrorMensaje = null;       // no es un error, es diferida
                nota.CodigoRecepcion = null;

                await _db.SaveUnitWork();

                _logger.LogInformation(
                    "Nota {Numero} (NotaId={NotaId}) diferida a contingencia. "
                  + "EventoSignificativoSiatId={EventoId}, Suc={Suc}, PV={PV}",
                    nota.NumeroNotaCreditoDebito, nota.Id, nota.EventoSignificativoSiatId,
                    ex.CodigoSucursal, ex.CodigoPuntoVenta);

                return new ResultadoEnvioNotaAjusteSiatDto
                {
                    Enviado = true,                 // frontend NO muestra error al cajero
                    Transaccion = false,            // el SIAT todavía no la validó
                    NotaAjusteId = nota.Id,
                    NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                    Cuf = nota.Cuf,
                    ErrorMensaje = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error de comunicación al enviar nota {Numero} (NotaId={NotaId}) al SIAT",
                    nota.NumeroNotaCreditoDebito, nota.Id);

                nota.EstadoSiat = FacturaEstado.Pendiente;
                nota.CodigoRecepcion = null;
                nota.ErrorMensaje = $"No se pudo enviar al SIAT: {ex.Message}";
                await _db.SaveUnitWork();

                return new ResultadoEnvioNotaAjusteSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    NotaAjusteId = nota.Id,
                    NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                    ErrorMensaje = nota.ErrorMensaje
                };
            }
        }

        /// <summary>
        /// Expande cada producto seleccionado en el PAR canónico SIAT (línea trans=1
        /// de referencia + línea trans=2 con la devolución efectiva).
        ///
        /// Por cada item del DTO:
        ///   • Línea A (trans=1): referencia al item original. Cantidad y SubTotal
        ///     se copian EXACTAMENTE del detalle original (regla SIAT 1049 — el SIAT
        ///     compara esta línea contra la línea original de la factura y rechaza
        ///     si difieren en cualquier campo, incluyendo cantidad/subTotal).
        ///   • Línea B (trans=2): devolución efectiva. Cantidad = cantidad
        ///     devuelta del DTO, subTotal = cant × precioUnitario.
        ///
        /// N items en la entrada ⇒ 2N líneas en el XML final (cumple XSD minOccurs=2).
        /// </summary>
        private static List<NotaAjusteDetalle> ExpandirParesTransaccion(
            List<DtoNotaAjusteDetalle> detallesDto,
            Dictionary<int, Detalle_Pago> detallesPorId,
            Dictionary<int, int> posicionPorId)
        {
            var resultado = new List<NotaAjusteDetalle>(detallesDto.Count * 2);
            foreach (var item in detallesDto)
            {
                if (!detallesPorId.TryGetValue(item.IdDetallePagoOriginal, out var original))
                    continue; // ya validado en el foreach anterior; blindamos por si cambia el flujo.

                var precioUnitario = original.PrecioUnitario;
                var lineaOriginal = posicionPorId[item.IdDetallePagoOriginal];

                // Línea A: referencia al item original (regla SIAT 1049 — debe coincidir
                // EXACTAMENTE con la factura original, así que copiamos Cantidad y
                // SubTotal del Detalle_Pago en vez de recalcularlos).
                resultado.Add(new NotaAjusteDetalle
                {
                    ActividadEconomica = original.ActividadEconomica,
                    CodigoProductoSin = original.CodigoProductoSin,
                    CodigoProducto = original.CodigoProducto,
                    Descripcion = original.Descripcion,
                    Cantidad = original.Cantidad,
                    UnidadMedida = original.UnidadMedida,
                    PrecioUnitario = original.PrecioUnitario,
                    SubTotal = original.SubTotal,
                    MontoDescuento = original.MontoDescuento ?? 0m,
                    CodigoDetalleTransaccion = 1,
                    IdDetallePagoOriginal = item.IdDetallePagoOriginal,
                    NumeroLineaOriginal = lineaOriginal
                });

                // Línea B: devolución efectiva.
                var subTotalTrans2 = Math.Round(item.Cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);
                resultado.Add(new NotaAjusteDetalle
                {
                    ActividadEconomica = original.ActividadEconomica,
                    CodigoProductoSin = original.CodigoProductoSin,
                    CodigoProducto = original.CodigoProducto,
                    Descripcion = original.Descripcion,
                    Cantidad = item.Cantidad,
                    UnidadMedida = original.UnidadMedida,
                    PrecioUnitario = precioUnitario,
                    SubTotal = subTotalTrans2,
                    MontoDescuento = item.MontoDescuento,
                    CodigoDetalleTransaccion = 2,
                    IdDetallePagoOriginal = item.IdDetallePagoOriginal,
                    NumeroLineaOriginal = lineaOriginal
                });
            }
            return resultado;
        }

        private static void AplicarResultadoSiat(NotaAjuste nota, RespuestaRecepcionNotaAjusteDto respuesta)
        {
            if (respuesta.Transaccion)
            {
                nota.EstadoSiat = FacturaEstado.Validada;
                nota.CodigoRecepcion = respuesta.CodigoRecepcion;
                nota.ErrorMensaje = null;
                return;
            }

            nota.EstadoSiat = FacturaEstado.Observada;
            nota.CodigoRecepcion = null;
            nota.ErrorMensaje = FormatearErroresSiat(respuesta);
        }

        private static string FormatearErroresSiat(RespuestaRecepcionNotaAjusteDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            return string.IsNullOrWhiteSpace(errores)
                ? respuesta.CodigoDescripcion ?? "El SIAT rechazó la nota sin detalle."
                : errores;
        }

        private static ResultadoEnvioNotaAjusteSiatDto MapearResultado(
            NotaAjuste nota,
            RespuestaRecepcionNotaAjusteDto respuesta,
            bool enviado = true)
        {
            return new ResultadoEnvioNotaAjusteSiatDto
            {
                Enviado = enviado,
                Transaccion = respuesta?.Transaccion ?? false,
                NotaAjusteId = nota.Id,
                NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                Cuf = nota.Cuf,
                CodigoEstado = respuesta?.CodigoEstado,
                CodigoRecepcion = nota.CodigoRecepcion,
                CodigoDescripcion = respuesta?.CodigoDescripcion,
                ErrorMensaje = nota.ErrorMensaje,
                CodigosRespuesta = respuesta?.CodigosRespuesta ?? new()
            };
        }
    }
}
