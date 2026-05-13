using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenItemProductoType : ObjectType<OrdenItemProducto>
    {
        protected override void Configure(IObjectTypeDescriptor<OrdenItemProducto> descriptor)
        {
            descriptor.Field(x => x.Producto).Type<OrdenProductoType>();
            descriptor.Field(x => x.Orden).Ignore();
        }
    }
}
