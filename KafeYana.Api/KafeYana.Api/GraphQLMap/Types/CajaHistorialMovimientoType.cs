using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CajaHistorialMovimientoType : ObjectType<CajaHistorialMovimiento>
    {
        protected override void Configure(IObjectTypeDescriptor<CajaHistorialMovimiento> descriptor)
        {
            descriptor.Field(x => x.CajaHistorial);
        }
    }
}
