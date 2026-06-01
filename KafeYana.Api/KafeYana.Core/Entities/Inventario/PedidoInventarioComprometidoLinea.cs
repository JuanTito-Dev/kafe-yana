using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities.Inventario;

public class PedidoInventarioComprometidoLinea : BaseEntity
{
    public int Id_Comprometido { get; set; }

    public required string TipoEntidad { get; set; }

    public int? Id_Producto { get; set; }

    public int? Id_Insumo { get; set; }

    public required int Cantidad { get; set; }

    public decimal Costo { get; set; }

    public required string Referencia { get; set; }

    public PedidoInventarioComprometido? Comprometido { get; set; }
}
