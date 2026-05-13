using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoPagos : IValidatableObject
    {
        [Range(0, double.MaxValue, ErrorMessage = "El pago en efectivo no puede ser negativo.")]
        public decimal Efectivo { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "El pago con tarjeta no puede ser negativo.")]
        public decimal Tarjeta { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "El pago por QR no puede ser negativo.")]
        public decimal Qr { get; set; } = 0;

        public decimal Total => Efectivo + Tarjeta + Qr;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Total <= 0)
                yield return new ValidationResult(
                    "Debe especificar al menos un monto de pago mayor a 0.",
                    new[] { nameof(Efectivo), nameof(Tarjeta), nameof(Qr) });
        }
    }
}
