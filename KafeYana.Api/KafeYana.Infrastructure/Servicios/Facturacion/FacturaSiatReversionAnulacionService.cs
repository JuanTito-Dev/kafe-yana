using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class FacturaSiatReversionAnulacionService(
        IReversionAnulacionFacturaService _reversionAnulacionFactura,
        IUnitWork _db,
        ILogger<FacturaSiatReversionAnulacionService> logger) : IFacturaSiatReversionAnulacionService
    {
        public async Task<ResultadoReversionAnulacionFacturaDto> RevertirAnulacionVentaAsync(
            int ventaId,
            CancellationToken ct = default)
        {
            var venta = await _db.ventas.FindByIdAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

            if (!venta.Facturado)
            {
                throw new VentaException(
                    "La venta no fue emitida como factura electrónica y no puede revertirse en el SIAT.");
            }

            if (venta.RevertidaAnulacion)
            {
                throw new VentaException(
                    "La anulación de esta factura ya fue revertida. El SIAT solo permite revertir una vez.");
            }

            if (venta.EstadoSiat != FacturaEstado.Anulada)
            {
                throw new VentaException(
                    "Solo se puede revertir la anulación de una factura con estado Anulada (950).");
            }

            if (string.IsNullOrWhiteSpace(venta.Cuf)
                || venta.Cuf.StartsWith("PENDIENTE", StringComparison.OrdinalIgnoreCase)
                || venta.Cuf.StartsWith("NF-", StringComparison.OrdinalIgnoreCase))
            {
                throw new VentaException("La venta no tiene un CUF válido para revertir la anulación en el SIAT.");
            }

            try
            {
                var solicitud = await _reversionAnulacionFactura.PrepararSolicitudAsync(
                    venta.Cuf,
                    venta.CodigoSucursal,
                    venta.CodigoPuntoVenta,
                    venta.CodigoDocumentoSector,
                    ct);

                var respuesta = await _reversionAnulacionFactura.EnviarReversionAsync(solicitud, ct);

                if (respuesta.Transaccion)
                {
                    venta.EstadoSiat = FacturaEstado.Validada;
                    venta.RevertidaAnulacion = true;
                    venta.ErrorMensaje = null;

                    logger.LogInformation(
                        "Anulación revertida en SIAT para factura {NumeroFactura} (VentaId={VentaId}). CUF={Cuf}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.Cuf);
                }
                else
                {
                    venta.ErrorMensaje = FormatearErroresSiat(respuesta);

                    logger.LogWarning(
                        "SIAT rechazó reversión de anulación de factura {NumeroFactura} (VentaId={VentaId}). {Error}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.ErrorMensaje);
                }

                await _db.SaveUnitWork();

                return new ResultadoReversionAnulacionFacturaDto
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
                    "Error de comunicación al revertir anulación de factura {NumeroFactura} (VentaId={VentaId}) en SIAT",
                    venta.NumeroFactura,
                    venta.Id);

                venta.ErrorMensaje = $"No se pudo revertir la anulación en el SIAT: {ex.Message}";
                await _db.SaveUnitWork();

                return new ResultadoReversionAnulacionFacturaDto
                {
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }
        }

        private static string FormatearErroresSiat(RespuestaReversionAnulacionFacturaDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la reversión de anulación sin detalle.";
        }
    }
}
