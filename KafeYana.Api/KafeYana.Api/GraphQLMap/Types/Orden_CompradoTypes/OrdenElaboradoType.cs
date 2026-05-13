using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types.Orden_CompradoTypes
{
    public class OrdenElaboradoType : ObjectType<Elaborado>
    {
        protected override void Configure(IObjectTypeDescriptor<Elaborado> descriptor)
        {
            descriptor.Field(x => x.Producto).Ignore();
            descriptor.Field(x => x.Receta).Ignore();
            descriptor.Field(x => x.Variaciones).Ignore();
        }
    }
}
