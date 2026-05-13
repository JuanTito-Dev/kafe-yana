using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class Detalle_Ronda_OpcionType : ObjectType<Detalle_Ronda_Opcion>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_Ronda_Opcion> descriptor)
        {
            descriptor.Field(x => x.Id_Detalle_Ronda).Type<IntType>();
            descriptor.Field(x => x.Id_Opcion).Type<IntType>();
            descriptor.Field(x => x.Opcion).Type<OpcionType>();
            descriptor.Field(x => x.Detalle_Ronda).Ignore();
        }
    }
}
