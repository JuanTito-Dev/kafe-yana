using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.PuntosDtos
{
    public class DtoAjusteManualPuntos
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Motivo { get; set; }
    }
}
