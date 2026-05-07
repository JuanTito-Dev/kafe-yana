using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class InsumoMovimientoType : ObjectType<InsumoMovimiento>
    {
        protected override void Configure(IObjectTypeDescriptor<InsumoMovimiento> descriptor)
        {
            descriptor.Field(x => x.Insumo).Ignore();
        }
    }
}
