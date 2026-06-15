using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class FacturaSiatEnvioService(
        IRecepcionFacturaService _recepcionFactura,
        IUnitWork _db,
        ILogger<FacturaSiatEnvioService> logger) : IFacturaSiatEnvioService
    {
        public async Task<ResultadoEnvioFacturaSiatDto> EnviarVentaAsync(
            Venta venta,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(venta.XmlBase64))
            {
                venta.EstadoSiat = FacturaEstado.Pendiente;
                venta.ErrorMensaje = "La venta no tiene archivo de factura para enviar al SIAT.";

                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }

            try
            {
                var hash = string.IsNullOrWhiteSpace(venta.CodigoHash) ? null : venta.CodigoHash;
                var respuesta = await _recepcionFactura.EnviarRecepcionAsync(venta.XmlBase64, hash, ct);
                AplicarResultadoSiat(venta, respuesta);

                if (!respuesta.Transaccion)
                {
                    logger.LogWarning(
                        "SIAT rechazó factura {NumeroFactura} (VentaId={VentaId}). {Error}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.ErrorMensaje);
                }
                else
                {
                    logger.LogInformation(
                        "Factura {NumeroFactura} (VentaId={VentaId}) validada por SIAT. CodigoRecepcion={Codigo}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.CodigoRecepcion);
                }

                return MapearResultado(venta, respuesta, enviado: true);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error de comunicación al enviar factura {NumeroFactura} (VentaId={VentaId}) al SIAT",
                    venta.NumeroFactura,
                    venta.Id);

                venta.EstadoSiat = FacturaEstado.Pendiente;
                venta.CodigoRecepcion = null;
                venta.ErrorMensaje = $"No se pudo enviar al SIAT: {ex.Message}";

                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }
        }

        public async Task<ResultadoEnvioFacturaSiatDto> ReenviarFacturaAsync(
            int ventaId,
            CancellationToken ct = default)
        {
            var venta = await _db.ventas.FindByIdAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

            if (venta.EstadoSiat == FacturaEstado.Anulada)
                throw new VentaException("No se puede reenviar al SIAT una venta anulada.");

            if (venta.EstadoSiat == FacturaEstado.Validada)
            {
                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = true,
                    EstadoSiat = venta.EstadoSiat,
                    CodigoRecepcion = venta.CodigoRecepcion,
                    CodigoEstado = (int)FacturaEstado.Validada,
                    CodigoDescripcion = "La factura ya está validada por el SIAT.",
                    ErrorMensaje = null
                };
            }

            if (string.IsNullOrWhiteSpace(venta.XmlBase64))
                throw new VentaException("La venta no tiene XML guardado para reenviar al SIAT.");

            if (string.IsNullOrWhiteSpace(venta.CodigoHash))
                throw new VentaException("La venta no tiene hash guardado para reenviar al SIAT.");

            var resultado = await EnviarVentaAsync(venta, ct);
            await _db.SaveUnitWork();
            return resultado;
        }

        private static void AplicarResultadoSiat(Venta venta, RespuestaRecepcionFacturaDto respuesta)
        {
            if (respuesta.Transaccion)
            {
                venta.EstadoSiat = FacturaEstado.Validada;
                venta.CodigoRecepcion = respuesta.CodigoRecepcion;
                venta.ErrorMensaje = null;
                return;
            }

            venta.EstadoSiat = FacturaEstado.Observada;
            venta.CodigoRecepcion = null;
            venta.ErrorMensaje = FormatearErroresSiat(respuesta);
        }

        private static string FormatearErroresSiat(RespuestaRecepcionFacturaDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la factura sin detalle.";
        }

        private static ResultadoEnvioFacturaSiatDto MapearResultado(
            Venta venta,
            RespuestaRecepcionFacturaDto respuesta,
            bool enviado)
        {
            return new ResultadoEnvioFacturaSiatDto
            {
                Enviado = enviado,
                Transaccion = respuesta.Transaccion,
                EstadoSiat = venta.EstadoSiat,
                CodigoEstado = respuesta.CodigoEstado,
                CodigoRecepcion = venta.CodigoRecepcion,
                CodigoDescripcion = respuesta.CodigoDescripcion,
                ErrorMensaje = venta.ErrorMensaje,
                CodigosRespuesta = respuesta.CodigosRespuesta
            };
        }
    }
}
