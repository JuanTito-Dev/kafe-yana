using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ProductoCanjeable
{
    public class DtoReclamarPromocionGratis
    {
        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdPromocionPermanente { get; set; }
    }
}
