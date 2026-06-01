using System.ComponentModel.DataAnnotations;

namespace KafeYana.Domain.Dtos.RondaDtos;

public class DtoRondadetalleEditar
{
    /// <summary>Si es null se crea una línea nueva en la ronda.</summary>
    public int? Id_Detalle { get; set; }

    [Required(ErrorMessage = "El campo Id_Producto es obligatorio.")]
    public int Id_Producto { get; set; }

    public List<int>? Ids_Opcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; }

    [MaxLength(500)]
    public string Nota { get; set; } = string.Empty;
}
