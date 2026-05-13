using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenInsumoType : ObjectType<Insumo>
    {
        protected override void Configure(IObjectTypeDescriptor<Insumo> descriptor)
        {
            descriptor.Field(x => x.Ajustes).Ignore();
            descriptor.Field(x => x.OrdenesInsumo).Ignore();
            descriptor.Field(x => x.Detalles).Ignore();
            descriptor.Field(x => x.Movimientos).Ignore();
        }
    }
}
