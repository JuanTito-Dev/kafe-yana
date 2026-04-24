using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class VentaType : ObjectType<Venta>
    {
        protected override void Configure(IObjectTypeDescriptor<Venta> descriptor)
        {
            descriptor.Field(v => v.Detalles).Type<ListType<DetalleVentaType>>()
                .Name("detalles")
                .Description("Lista de detalles de la venta");
        }
    }
}
