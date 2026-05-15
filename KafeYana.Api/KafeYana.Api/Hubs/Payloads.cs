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
}
