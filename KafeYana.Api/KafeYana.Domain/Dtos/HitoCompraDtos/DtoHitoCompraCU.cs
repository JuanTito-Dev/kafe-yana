using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.HitoCompraDtos
{
    public class DtoHitoCompraCU
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "NumeroCompras debe ser mayor a 0")]
        public int NumeroCompras { get; set; }

        [Required]
        public int Id_ProductoCanjeable { get; set; }

        [Required]
        [MaxLength(500)]
        public required string Descripcion { get; set; }

        [Required]
        [MaxLength(500)]
        public required string Icono { get; set; }

        [Required]
        public bool Activo { get; set; }
    }
}
