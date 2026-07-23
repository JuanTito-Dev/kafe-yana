using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class VentaType : ObjectType<Venta>
    {
        protected override void Configure(IObjectTypeDescriptor<Venta> descriptor)
        {
            descriptor.Field(v => v.Detalles).Type<ListType<DetallePagoType>>()
                .Name("detalles")
                .Description("Líneas de detalle de la factura");

            // ── Cantidad de líneas (derivado, no carga la colección) ─────
            // EF traduce `Detalles.Count` a un subquery SQL, así que la lista
            // puede pedir sólo este conteo sin traer todas las filas de
            // Detalle_Pago. El modal pide `detalles` por separado cuando
            // necesita las líneas completas.
            descriptor.Field(v => v.Detalles.Count).Type<IntType>()
                .Name("cantidadProductos")
                .Description("Cantidad de líneas de detalle de la factura (no carga los detalles).");
        }
    }
}
