using System.ComponentModel.DataAnnotations;

namespace KafeYana.Domain.Dtos.RondaDtos;

public class DtoRondaEditar
{
    [Required]
    public int Id_Pedido { get; set; }

    [MinLength(1, ErrorMessage = "Debe incluir al menos un detalle.")]
    public List<DtoRondadetalleEditar> Detalles { get; set; } = [];
}
