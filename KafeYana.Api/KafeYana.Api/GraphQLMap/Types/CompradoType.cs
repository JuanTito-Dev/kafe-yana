using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CompradoType : ObjectType<Comprado>
    {
        protected override void Configure(IObjectTypeDescriptor<Comprado> descriptor)
        {
            descriptor.Field(x => x.Id).Ignore();

            descriptor.Field(x => x.Codigo_barra);

            descriptor.Field(x => x.Unidad_medida);

            descriptor.Field(x => x.Marca);

            descriptor.Field(x => x.Ubicacion);

            descriptor.Field(x => x.Costo_compra);

            descriptor.Field(x => x.Stock_actual);

            descriptor.Field(x => x.Stock_minimo);

            descriptor.Field(x => x.Disponible);

            descriptor.Field(x => x.Id_Producto).Ignore();

            descriptor.Field(x => x.Producto).Type<ProductoType>();
        }
    }
}
