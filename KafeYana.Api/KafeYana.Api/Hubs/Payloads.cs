namespace KafeYana.Api.Hubs
{
    public record MesaActualizadaPayload(
        int    Id,
        string Nombre,
        bool   Disponible,
        int?   IdPedido
    );

    public record NuevaRondaPayload(
        string                         NombreMesa,
        int                            NumeroOrden,
        int                            RondaId,
        string                         RondaDescripcion,
        decimal                        SubTotal,
        IEnumerable<RondaDetalleItem>  Detalles
    );

    public record RondaDetalleItem(
        string                        Nombre,
        int                           Cantidad,
        decimal                       Precio,
        string                        Ubicacion,
        IEnumerable<OpcionItem>       Opciones,
        IEnumerable<ComboItem>        ItemsCombo
    );

    public record OpcionItem(
        string                  Nombre,
        decimal                 AjustePrecio,
        IEnumerable<CambioItem> Cambios
    );

    public record CambioItem(
        string  Tipo,
        string  Sale,
        string? Entra,
        decimal Cantidad,
        string  Unidad
    );

    public record ComboItem(
        string  Nombre,
        int     Cantidad,
        string  Ubicacion
    );

    public record VentaPayload(
        string  NombreMesa,
        int     NumeroOrden,
        decimal Total
    );

    public record ParaLlevarPayload(
        int?  IdPedido,
        bool  Disponible
    );

    // ── Stock ────────────────────────────────────────────────────────────────

    public record StockActualizadoPayload(
        IEnumerable<CompradoStockItem>  Comprados,
        IEnumerable<ElaboradoStockItem> Elaborados,
        IEnumerable<ComboStockItem>     Combos
    );

    /// <param name="Id">Id del Producto</param>
    /// <param name="Stock">Stock actual post-venta</param>
    public record CompradoStockItem(int Id, int Stock);

    /// <param name="Id">Id del Producto</param>
    /// <param name="Stock">Stock actual (solo cuando Producible=true, si no es 0)</param>
    /// <param name="CantidadProducible">Cuántas porciones se pueden preparar ahora (solo cuando Producible=false con receta)</param>
    public record ElaboradoStockItem(int Id, int Stock, int? CantidadProducible);

    /// <param name="Id">Id del Producto combo</param>
    /// <param name="CantidadProducible">Cuántos combos se pueden armar con el stock actual de sus componentes</param>
    public record ComboStockItem(int Id, int CantidadProducible);
}
