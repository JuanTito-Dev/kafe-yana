using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities.Inventario;

public class PedidoInventarioComprometido : BaseEntity
{
    public required int Id_Pedido { get; set; }

    public int Id_Detalle_Ronda { get; set; }

    public required string Referencia { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Detalle_ronda? DetalleRonda { get; set; }

    public List<PedidoInventarioComprometidoLinea> Lineas { get; set; } = [];
}
