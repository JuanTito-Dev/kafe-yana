using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoVentaPedido : IValidatableObject
    {
        [Required]
        public required int Id_Pedido { get; set; }

        [Required]
        public required int Id_Cliente { get; set; }

        [Required]
        public required DtoPagos Pagos { get; set; }

        /// <summary>Si es true, aplica el mejor descuento permanente disponible. Default: false.</summary>
        public bool AplicarDescuentos { get; set; } = false;

        /// <summary>Paramétrica SIAT codigoTipoDocumentoIdentidad (1 a 5).</summary>
        [Required(ErrorMessage = "El código de tipo de documento es requerido.")]
        [Range(1, 5, ErrorMessage = "El código de tipo de documento debe estar entre 1 y 5.")]
        public required int CodigoTipoDocumento { get; set; }

        /// <summary>Número del documento de identidad del comprador.</summary>
        [Required(ErrorMessage = "El número de documento es requerido.")]
        [MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres.")]
        public required string NumeroDocumento { get; set; }

        /// <summary>Complemento SEGIP. Opcional; null si no aplica.</summary>
        [MaxLength(10, ErrorMessage = "El complemento no puede exceder 10 caracteres.")]
        public string? Complemento { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.IsDefined(typeof(TipoDocumentoIdentidadSiat), CodigoTipoDocumento))
            {
                yield return new ValidationResult(
                    "El código de tipo de documento no es válido. Valores permitidos: 1 (CI), 2 (CEX), 3 (PAS), 4 (OD), 5 (NIT).",
                    [nameof(CodigoTipoDocumento)]);
            }

            if (string.IsNullOrWhiteSpace(NumeroDocumento))
            {
                yield return new ValidationResult(
                    "El número de documento no puede estar vacío.",
                    [nameof(NumeroDocumento)]);
            }
        }
    }
}
