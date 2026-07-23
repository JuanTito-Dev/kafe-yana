using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Body SOAP <c>anulacionDocumentoAjuste</c>. Orden de campos estricto del
    /// WSDL: codigoAmbiente, codigoDocumentoSector, codigoEmision, codigoModalidad,
    /// codigoPuntoVenta, codigoSistema, codigoSucursal, cufd, cuis, nit,
    /// tipoFacturaDocumento, codigoMotivo, cuf.
    /// </summary>
    public class SolicitudAnulacionDocumentoAjusteDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoDocumentoSector { get; set; }
        public int CodigoEmision { get; set; }
        public int CodigoModalidad { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int TipoFacturaDocumento { get; set; }
        public int CodigoMotivo { get; set; }
        public string Cuf { get; set; } = string.Empty;
    }

    public class RespuestaAnulacionDocumentoAjusteDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    public sealed class ResultadoAnulacionNotaAjusteDto
    {
        public bool Transaccion { get; init; }
        public int? CodigoEstado { get; init; }
        public string? CodigoDescripcion { get; init; }
        public FacturaEstado? EstadoSiat { get; init; }
        public string? ErrorMensaje { get; init; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; init; } = new();
    }

    /// <summary>
    /// Body SOAP <c>reversionAnulacionDocumentoAjuste</c>. Igual a la solicitud
    /// de anulación pero SIN <c>codigoMotivo</c> (el SIN no lo exige para revertir).
    /// </summary>
    public class SolicitudReversionAnulacionDocumentoAjusteDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoDocumentoSector { get; set; }
        public int CodigoEmision { get; set; }
        public int CodigoModalidad { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int TipoFacturaDocumento { get; set; }
        public string Cuf { get; set; } = string.Empty;
    }

    public class RespuestaReversionAnulacionDocumentoAjusteDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    public sealed class ResultadoReversionAnulacionNotaAjusteDto
    {
        public bool Transaccion { get; init; }
        public int? CodigoEstado { get; init; }
        public string? CodigoDescripcion { get; init; }
        public FacturaEstado? EstadoSiat { get; init; }
        public string? ErrorMensaje { get; init; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; init; } = new();
    }

    /// <summary>
    /// Body del endpoint POST /NotaAjuste/anular/{id}. Valida que el motivo
    /// pertenezca al catálogo vigente (BD o fallback hardcoded).
    /// </summary>
    public class DtoAnularNotaAjuste : IValidatableObject
    {
        [Required, Range(1, 99)]
        public int CodigoMotivo { get; set; }

        /// <summary>Nota libre del operador. No se envía al SIAT (igual que en factura).</summary>
        public string? Nota { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!MotivoAnulacionSiatCatalogo.EsValido(CodigoMotivo))
            {
                yield return new ValidationResult(
                    "Motivo de anulación no válido.",
                    new[] { nameof(CodigoMotivo) });
            }
        }
    }
}