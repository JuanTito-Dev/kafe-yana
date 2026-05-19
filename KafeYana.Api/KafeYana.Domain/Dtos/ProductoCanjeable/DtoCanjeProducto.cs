using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ProductoCanjeable
{
    public class DtoCanjeProducto
    {
        [Required]
        public int IdProductoCanjeable { get; set; }

        [Required]
        public int IdCliente { get; set; }
    }
}
