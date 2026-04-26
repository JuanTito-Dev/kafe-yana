using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleRondaOpcionType : ObjectType<Detalle_Ronda_Opcion>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_Ronda_Opcion> descriptor)
        {
            descriptor.Field(x => x.Id_Opcion);
            descriptor.Field(x => x.Opcion).Type<OpcionType>();
        }
    }
}