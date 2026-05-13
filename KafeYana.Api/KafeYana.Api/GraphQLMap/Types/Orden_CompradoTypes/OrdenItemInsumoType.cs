using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenItemInsumoType : ObjectType<OrdenItemInsumo>
    {
        protected override void Configure(IObjectTypeDescriptor<OrdenItemInsumo> descriptor)
        {
            descriptor.Field(x => x.Insumo).Type<OrdenInsumoType>();
        }
    }
}
