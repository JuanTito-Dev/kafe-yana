using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenProductoType : ObjectType<Producto>
    {
        protected override void Configure(IObjectTypeDescriptor<Producto> descriptor)
        {
            descriptor.Field(x => x.Movimientos).Ignore();
            descriptor.Field(x => x.Comprado).Type<OrdenElaboradoType>();
            descriptor.Field(x => x.Comprado).Ignore();
            descriptor.Field(x => x.Promocion).Ignore();
            descriptor.Field(x => x.Categoria).Ignore();
        }
    }
}
