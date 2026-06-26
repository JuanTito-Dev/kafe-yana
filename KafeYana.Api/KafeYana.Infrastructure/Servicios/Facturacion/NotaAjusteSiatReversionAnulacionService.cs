using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Orquestador de alto nivel para reversión de anulación de notas C/D.
    /// Espejo de <see cref="FacturaSiatReversionAnulacionService"/>.
    /// </summary>
    public class NotaAjusteSiatReversionAnulacionService(
        INotaAjusteReversionAnulacionService _reversionAnulacionNota,
        IUnitWork _db,
        ILogger<NotaAjusteSiatReversionAnulacionService> logger) : INotaAjusteSiatReversionAnulacionService
    {
        public async Task<ResultadoReversionAnulacionNotaAjusteDto> RevertirAnulacionNotaAsync(
            int notaId,
            CancellationToken ct = default)
        {
            var nota = await _db.notasAjuste.FindByIdAsync(notaId);
            if (nota is null)
                throw new VentaException("Nota no encontrada.");

            if (nota.RevertidaAnulacion)
            {
                throw new VentaException(
                    "La anulación de esta nota ya fue revertida. El SIAT solo permite revertir una vez.");
            }

            if (nota.EstadoSiat != FacturaEstado.Anulada)
            {
                throw new VentaException(
                    "Solo se puede revertir la anulación de una nota con estado Anulada (950).");
            }

            if (string.IsNullOrWhiteSpace(nota.Cuf)
                || nota.Cuf.StartsWith("PENDIENTE", StringComparison.OrdinalIgnoreCase))
            {
                throw new VentaException("La nota no tiene un CUF válido para revertir la anulación en el SIAT.");
            }

            try
            {
                var solicitud = await _reversionAnulacionNota.PrepararSolicitudAsync(notaId, ct);
                var respuesta = await _reversionAnulacionNota.EnviarReversionAsync(solicitud, ct);

                if (respuesta.Transaccion)
                {
                    nota.EstadoSiat = FacturaEstado.Validada;
                    nota.RevertidaAnulacion = true;
                    nota.FechaAnulacionSiat = null; // ya no está anulada
                    nota.ErrorMensaje = null;

                    logger.LogInformation(
                        "Anulación revertida en SIAT para nota {Numero} (NotaId={NotaId}). CUF={Cuf}",
                        nota.NumeroNotaCreditoDebito,
                        nota.Id,
                        nota.Cuf);
                }
                else
                {
                    nota.ErrorMensaje = FormatearErroresSiat(respuesta);

                    logger.LogWarning(
                        "SIAT rechazó reversión de anulación de nota {Numero} (NotaId={NotaId}). {Error}",
                        nota.NumeroNotaCreditoDebito,
                        nota.Id,
                        nota.ErrorMensaje);
                }

                await _db.SaveUnitWork();

                return new ResultadoReversionAnulacionNotaAjusteDto
                {
                    Transaccion = respuesta.Transaccion,
                    CodigoEstado = respuesta.CodigoEstado,
                    CodigoDescripcion = respuesta.CodigoDescripcion,
                    EstadoSiat = nota.EstadoSiat,
                    ErrorMensaje = nota.ErrorMensaje,
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
                    "Error de comunicación al revertir anulación de nota {Numero} (NotaId={NotaId}) en SIAT",
                    nota.NumeroNotaCreditoDebito,
                    nota.Id);

                nota.ErrorMensaje = $"No se pudo revertir la anulación en el SIAT: {ex.Message}";
                await _db.SaveUnitWork();

                return new ResultadoReversionAnulacionNotaAjusteDto
                {
                    Transaccion = false,
                    EstadoSiat = nota.EstadoSiat,
                    ErrorMensaje = nota.ErrorMensaje
                };
            }
        }

        private static string FormatearErroresSiat(RespuestaReversionAnulacionDocumentoAjusteDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la reversión de anulación sin detalle.";
        }
    }
}