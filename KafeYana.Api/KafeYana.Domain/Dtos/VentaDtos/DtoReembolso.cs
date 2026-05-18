using KafeYana.Domain.TiposDeDatos;
using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoReembolso
    {
        [Required(ErrorMessage = "El tipo de pago es requerido")]
        public required TipoPagos TipoPago { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero")]
        public required decimal Monto { get; set; }

        [Required(ErrorMessage = "El motivo es requerido")]
        public required string Motivo { get; set; }
    }
}
