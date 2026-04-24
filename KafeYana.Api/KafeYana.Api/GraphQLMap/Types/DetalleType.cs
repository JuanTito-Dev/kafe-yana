using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleType : ObjectType<Detalle>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle> descriptor)
        {
            descriptor.Field(x => x.Insumo).Type<InsumoType>();
        }
    }
}
