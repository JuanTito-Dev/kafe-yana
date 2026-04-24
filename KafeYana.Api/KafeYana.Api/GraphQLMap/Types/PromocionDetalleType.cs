using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class PromocionDetalleType : ObjectType<PromocionDetalle>
    {
        protected override void Configure(IObjectTypeDescriptor<PromocionDetalle> descriptor)
        {
            descriptor.Field(x => x.Id_Producto).Ignore();
            descriptor.Field(x => x.Id_Promocion).Ignore();
            descriptor.Field(x => x.Producto).Type<ProductoType>();
            descriptor.Field(x => x.Promocion).Ignore();
        }
    }
}
