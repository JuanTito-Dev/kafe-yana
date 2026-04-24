using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleVentaType : ObjectType<Detalle_venta>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_venta> descriptor)
        {
            descriptor.Field(d => d.venta).Ignore();
        }
    }
}
