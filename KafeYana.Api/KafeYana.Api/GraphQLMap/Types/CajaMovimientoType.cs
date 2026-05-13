using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CajaMovimientoType : ObjectType<CajaMovimiento>
    {
        protected override void Configure(IObjectTypeDescriptor<CajaMovimiento> descriptor)
        {
            descriptor.Field(x => x.Caja).Ignore();
        }
    }
}
