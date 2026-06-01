namespace KafeYana.Domain.Dtos.InventarioPedido;

public sealed class CompromisoLineaCalculo
{
    public required string TipoEntidad { get; init; }

    public int? Id_Producto { get; init; }

    public int? Id_Insumo { get; init; }

    public required int Cantidad { get; init; }

    public decimal Costo { get; init; }

    public required string Referencia { get; init; }
}
