using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
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
        private readonly ILogger<NotaAjusteSiatEnvioService> _logger;

        public NotaAjusteSiatEnvioService(
            IUnitWork db,
            INotaAjusteSiatPreparer preparer,
            IRecepcionNotaAjusteService recepcionNota,
            ILogger<NotaAjusteSiatEnvioService> logger)
        {
            _db = db;
            _preparer = preparer;
            _recepcionNota = recepcionNota;
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

            foreach (var item in dto.Detalles)
            {
                if (!detallesPorId.TryGetValue(item.IdDetallePagoOriginal, out var original))
                    throw new VentaException(
                        $"DetallePago {item.IdDetallePagoOriginal} no pertenece a la venta {ventaId}.");

                // La cantidad a devolver no puede exceder la cantidad original
                if (item.Cantidad > original.Cantidad)
                    throw new VentaException(
                        $"Cantidad a devolver ({item.Cantidad}) del producto '{original.Descripcion}' "
                        + $"excede la cantidad facturada ({original.Cantidad}).");
            }

            // Calcular totales según reglas SIAT Bolivia (notaComputarizadaCreditoDebito.xsd)
            // Ver [[kafeyana-notaajuste-siat-reglas]] para el detalle de los códigos 1029/1030/1031/1049.
            //
            // Reglas clave (validadas contra rechazos reales del SIAT):
            //   • montoTotalDevuelto = suma de subTotal de los detalles de la nota
            //   • montoTotalOriginal = suma de subTotal de los detalles de la nota
            //     (NO venta.MontoTotal — para devoluciones parciales el SIAT compara
            //      contra lo que esta nota está ajustando, no contra la factura entera)
            //   • montoEfectivoCreditoDebito = montoTotalDevuelto * 0.13
            //     (es el IVA componente / crédito fiscal, NO el total devuelto)
            //   • montoDescuentoCreditoDebito = descuento global aplicado (si hay)
            var sumaSubtotales = dto.Detalles.Sum(x => x.SubTotal);
            var descuento = dto.MontoDescuentoCreditoDebito ?? 0m;
            var montoDevueltoNeto = sumaSubtotales - descuento;

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
                MontoTotalOriginal = sumaSubtotales,
                MontoTotalDevuelto = sumaSubtotales,
                MontoDescuentoCreditoDebito = descuento,
                MontoEfectivoCreditoDebito = Math.Round(montoDevueltoNeto * 0.13m, 2, MidpointRounding.AwayFromZero),

                // Catálogo
                CodigoMotivoAjuste = dto.CodigoMotivoAjuste,

                // Generado luego por el preparer
                NumeroNotaCreditoDebito = await _db.notasAjuste.SiguienteNumeroNotaCreditoDebitoAsync(),

                // Placeholders hasta que el preparer los asigne
                Cuf = "PENDIENTE",
                Cufd = "PENDIENTE",
                Leyenda = string.Empty,
                Usuario = string.IsNullOrWhiteSpace(dto.Usuario) ? "SISTEMA" : dto.Usuario,

                Detalles = dto.Detalles.Select((item, idx) =>
                {
                    var original = detallesPorId[item.IdDetallePagoOriginal];
                    // Regla SIAT 1049: el detalle de la nota debe coincidir con la factura
                    // original en codigoProducto, precioUnitario, unidadMedida, actividadEconomica.
                    // El frontend puede enviar precioUnitario/subTotal distintos, pero el SIAT
                    // rechaza si no coinciden exactamente. Por eso forzamos desde `original`.
                    var precioUnitario = original.PrecioUnitario;
                    var subTotal = Math.Round(item.Cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);
                    return new NotaAjusteDetalle
                    {
                        ActividadEconomica = original.ActividadEconomica,
                        CodigoProductoSin = original.CodigoProductoSin,
                        CodigoProducto = original.CodigoProducto,
                        Descripcion = original.Descripcion,
                        Cantidad = item.Cantidad,
                        UnidadMedida = original.UnidadMedida,
                        PrecioUnitario = precioUnitario,
                        SubTotal = subTotal,
                        MontoDescuento = item.MontoDescuento,
                        CodigoDetalleTransaccion = item.CodigoDetalleTransaccion,
                        IdDetallePagoOriginal = item.IdDetallePagoOriginal,
                        NumeroLineaOriginal = posicionPorId[item.IdDetallePagoOriginal]
                    };
                }).ToList()
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
                var respuesta = await _recepcionNota.EnviarRecepcionAsync(nota.XmlBase64, hash, ct);
                AplicarResultadoSiat(nota, respuesta);

                await _db.SaveUnitWork();

                if (!respuesta.Transaccion)
                    _logger.LogWarning(
                        "SIAT rechazó nota {Numero} (NotaId={NotaId}). {Error}",
                        nota.NumeroNotaCreditoDebito, nota.Id, nota.ErrorMensaje);
                else
                    _logger.LogInformation(
                        "Nota {Numero} (NotaId={NotaId}) validada por SIAT. CodigoRecepcion={Codigo}",
                        nota.NumeroNotaCreditoDebito, nota.Id, nota.CodigoRecepcion);

                return MapearResultado(nota, respuesta);
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
            RespuestaRecepcionNotaAjusteDto respuesta)
        {
            return new ResultadoEnvioNotaAjusteSiatDto
            {
                Enviado = true,
                Transaccion = respuesta.Transaccion,
                NotaAjusteId = nota.Id,
                NumeroNotaCreditoDebito = (int)nota.NumeroNotaCreditoDebito,
                Cuf = nota.Cuf,
                CodigoEstado = respuesta.CodigoEstado,
                CodigoRecepcion = nota.CodigoRecepcion,
                CodigoDescripcion = respuesta.CodigoDescripcion,
                ErrorMensaje = nota.ErrorMensaje,
                CodigosRespuesta = respuesta.CodigosRespuesta
            };
        }
    }
}
