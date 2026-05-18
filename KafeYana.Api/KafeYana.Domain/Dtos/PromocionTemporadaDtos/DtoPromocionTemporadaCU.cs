using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.PromocionTemporadaDtos
{
    public class DtoPromocionTemporadaCU
    {
        [Required]
        [MaxLength(120)]
        public required string Nombre { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        public List<int> IdsProductosCanjeables { get; set; } = [];

        [Required]
        public bool Activo { get; set; }
    }
}
