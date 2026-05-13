using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenCompraType : ObjectType<OrdenCompra>
    {
        protected override void Configure(IObjectTypeDescriptor<OrdenCompra> descriptor)
        {
            descriptor.Field(x => x.Insumos).Type<ListType<OrdenItemInsumoType>>();
            descriptor.Field(x => x.Productos).Type<ListType<OrdenItemProductoType>>();
        }
    }
}
