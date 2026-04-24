using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class InsumoType : ObjectType<Insumo>
    {
        protected override void Configure(IObjectTypeDescriptor<Insumo> descriptor)
        {
            descriptor.Field(x => x.Nombre);
            descriptor.Field(x => x.Categoria);
            descriptor.Field(x => x.Unidad_min_uso);
            descriptor.Field(x => x.Unidad_compra);
            descriptor.Field(x => x.Factor_conversion);
            descriptor.Field(x => x.Costo);
            descriptor.Field(x => x.Stock_actual);
            descriptor.Field(x => x.Stock_min);
            descriptor.Field(x => x.Ajustes).Ignore();
            descriptor.Field(x => x.Detalles).Ignore();
        }
    }
}
