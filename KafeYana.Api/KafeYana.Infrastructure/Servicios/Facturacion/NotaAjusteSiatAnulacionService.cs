using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Orquestador de alto nivel para anulación de notas C/D contra el SIAT.
    /// Espejo de <see cref="FacturaSiatAnulacionService"/>.
    ///
    /// Reglas de negocio:
    ///   • El motivo debe pertenecer al catálogo vigente del SIAT.
    ///   • La nota debe estar Validada (908). Las demás se rechazan o son no-ops.
    ///   • Si la anulación ya fue revertida, no se puede volver a anular.
    ///   • El CUF debe estar completo (no PENDIENTE).
    /// </summary>
    public class NotaAjusteSiatAnulacionService(
        INotaAjusteAnulacionService _anulacionNota,
        IUnitWork _db,
        ILogger<NotaAjusteSiatAnulacionService> logger) : INotaAjusteSiatAnulacionService
    {
        public async Task<ResultadoAnulacionNotaAjusteDto> AnularNotaAsync(
            int notaId,
            int codigoMotivo,
            CancellationToken ct = default)
        {
            if (!MotivoAnulacionSiatCatalogo.EsValido(codigoMotivo))
            {
                throw new VentaException(
                    "El código de motivo de anulación no es válido. Valores permitidos: 1, 2, 3, 4.");
            }

            var nota = await _db.notasAjuste.FindByIdAsync(notaId);
            if (nota is null)
                throw new VentaException("Nota no encontrada.");

            if (nota.RevertidaAnulacion)
            {
                throw new VentaException(
                    "No se puede anular en el SIAT una nota cuya anulación ya fue revertida.");
            }

            if (nota.EstadoSiat == FacturaEstado.Anulada)
            {
                return new ResultadoAnulacionNotaAjusteDto
                {
                    Transaccion = true,
                    CodigoEstado = (int)FacturaEstado.Anulada,
                    CodigoDescripcion = "La nota ya está anulada.",
                    EstadoSiat = nota.EstadoSiat
                };
            }

            if (nota.EstadoSiat != FacturaEstado.Validada)
            {
                throw new VentaException(
                    "Solo se puede anular en el SIAT una nota con estado Validada (908).");
            }

            if (string.IsNullOrWhiteSpace(nota.Cuf)
                || nota.Cuf.StartsWith("PENDIENTE", StringComparison.OrdinalIgnoreCase))
            {
                throw new VentaException("La nota no tiene un CUF válido para anular en el SIAT.");
            }

            try
            {
                var solicitud = await _anulacionNota.PrepararSolicitudAsync(notaId, codigoMotivo, ct);
                var respuesta = await _anulacionNota.EnviarAnulacionAsync(solicitud, ct);

                if (respuesta.Transaccion)
                {
                    nota.EstadoSiat = FacturaEstado.Anulada;
                    nota.FechaAnulacionSiat = DateTime.UtcNow;
                    nota.ErrorMensaje = null;

                    logger.LogInformation(
                        "Nota {Numero} (NotaId={NotaId}) anulada en SIAT. CUF={Cuf}",
                        nota.NumeroNotaCreditoDebito,
                        nota.Id,
                        nota.Cuf);
                }
                else
                {
                    nota.ErrorMensaje = FormatearErroresSiat(respuesta);

                    logger.LogWarning(
                        "SIAT rechazó anulación de nota {Numero} (NotaId={NotaId}). {Error}",
                        nota.NumeroNotaCreditoDebito,
                        nota.Id,
                        nota.ErrorMensaje);
                }

                await _db.SaveUnitWork();

                return new ResultadoAnulacionNotaAjusteDto
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
                    "Error de comunicación al anular nota {Numero} (NotaId={NotaId}) en SIAT",
                    nota.NumeroNotaCreditoDebito,
                    nota.Id);

                nota.ErrorMensaje = $"No se pudo anular en el SIAT: {ex.Message}";
                await _db.SaveUnitWork();

                return new ResultadoAnulacionNotaAjusteDto
                {
                    Transaccion = false,
                    EstadoSiat = nota.EstadoSiat,
                    ErrorMensaje = nota.ErrorMensaje
                };
            }
        }

        private static string FormatearErroresSiat(RespuestaAnulacionDocumentoAjusteDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la anulación sin detalle.";
        }
    }
}