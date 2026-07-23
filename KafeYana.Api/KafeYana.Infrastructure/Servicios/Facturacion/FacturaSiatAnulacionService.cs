using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class FacturaSiatAnulacionService(
        IAnulacionFacturaService _anulacionFactura,
        IUnitWork _db,
        ILogger<FacturaSiatAnulacionService> logger) : IFacturaSiatAnulacionService
    {
        public async Task<ResultadoAnulacionFacturaDto> AnularVentaAsync(
            int ventaId,
            int codigoMotivo,
            CancellationToken ct = default)
        {
            if (!MotivoAnulacionSiatCatalogo.EsValido(codigoMotivo))
            {
                throw new VentaException(
                    "El código de motivo de anulación no es válido. Valores permitidos: 1, 2, 3, 4.");
            }

            var venta = await _db.ventas.FindByIdAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

            if (!venta.Facturado)
                throw new VentaException("La venta no fue emitida como factura electrónica y no puede anularse en el SIAT.");

            if (venta.RevertidaAnulacion)
            {
                throw new VentaException(
                    "No se puede anular en el SIAT una factura cuya anulación ya fue revertida.");
            }

            if (venta.EstadoSiat == FacturaEstado.Anulada)
            {
                return new ResultadoAnulacionFacturaDto
                {
                    Transaccion = true,
                    CodigoEstado = (int)FacturaEstado.Anulada,
                    CodigoDescripcion = "La factura ya está anulada.",
                    EstadoSiat = venta.EstadoSiat
                };
            }

            if (venta.EstadoSiat != FacturaEstado.Validada)
            {
                throw new VentaException(
                    "Solo se puede anular en el SIAT una factura con estado Validada (908).");
            }

            if (string.IsNullOrWhiteSpace(venta.Cuf))
                throw new VentaException("La venta no tiene CUF registrado para anular en el SIAT.");

            // ── Cascada obligatoria: anular notas antes que la factura ─────
            // El SIN no permite "desanular" una factura, pero si lo permitiera
            // y la factura madre se anula, las notas C/D quedan huérfanas
            // (documentos válidos que referencian una factura inexistente).
            // Regla: NO se puede anular una factura mientras tenga notas
            // activas (cualquier estado ≠ Anulada, incluyendo Pendiente/
            // Observada por seguridad). El usuario debe anular primero cada
            // nota y recién después la factura.
            var notasActivas = (await _db.notasAjuste.ListarPorVentaAsync(ventaId))
                .Count(n => n.EstadoSiat != FacturaEstado.Anulada);

            if (notasActivas > 0)
            {
                throw new VentaException(
                    $"La factura tiene {notasActivas} nota(s) de ajuste vinculada(s). "
                  + "Debe anular primero las notas antes de anular la factura en el SIAT.");
            }

            try
            {
                var solicitud = await _anulacionFactura.PrepararSolicitudAsync(
                    venta.Cuf,
                    codigoMotivo,
                    venta.CodigoSucursal,
                    venta.CodigoPuntoVenta,
                    venta.CodigoDocumentoSector,
                    ct);

                var respuesta = await _anulacionFactura.EnviarAnulacionAsync(solicitud, ct);

                if (respuesta.Transaccion)
                {
                    venta.EstadoSiat = FacturaEstado.Anulada;
                    venta.ErrorMensaje = null;

                    logger.LogInformation(
                        "Factura {NumeroFactura} (VentaId={VentaId}) anulada en SIAT. CUF={Cuf}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.Cuf);
                }
                else
                {
                    venta.ErrorMensaje = FormatearErroresSiat(respuesta);

                    logger.LogWarning(
                        "SIAT rechazó anulación de factura {NumeroFactura} (VentaId={VentaId}). {Error}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.ErrorMensaje);
                }

                await _db.SaveUnitWork();

                return new ResultadoAnulacionFacturaDto
                {
                    Transaccion = respuesta.Transaccion,
                    CodigoEstado = respuesta.CodigoEstado,
                    CodigoDescripcion = respuesta.CodigoDescripcion,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje,
                    CodigosRespuesta = respuesta.CodigosRespuesta
                };
            }
            catch (VentaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error de comunicación al anular factura {NumeroFactura} (VentaId={VentaId}) en SIAT",
                    venta.NumeroFactura,
                    venta.Id);

                venta.ErrorMensaje = $"No se pudo anular en el SIAT: {ex.Message}";
                await _db.SaveUnitWork();

                return new ResultadoAnulacionFacturaDto
                {
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }
        }

        private static string FormatearErroresSiat(RespuestaAnulacionFacturaDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la anulación sin detalle.";
        }
    }
}
