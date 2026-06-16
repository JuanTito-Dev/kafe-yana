using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public class DtoAnularFactura : IValidatableObject
    {
        [Required(ErrorMessage = "El código de motivo de anulación es requerido.")]
        [Range(1, 99, ErrorMessage = "El código de motivo debe ser un valor numérico válido.")]
        public required int CodigoMotivo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!MotivoAnulacionSiatCatalogo.EsValido(CodigoMotivo))
            {
                yield return new ValidationResult(
                    "El código de motivo de anulación no es válido. Valores permitidos: 1, 2, 3, 4.",
                    [nameof(CodigoMotivo)]);
            }
        }
    }
}
