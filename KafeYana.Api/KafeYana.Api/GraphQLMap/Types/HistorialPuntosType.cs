using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class HistorialPuntosType : ObjectType<HistorialPuntos>
    {
        protected override void Configure(IObjectTypeDescriptor<HistorialPuntos> descriptor)
        {
            descriptor.Field(x => x.Cliente).Ignore();
        }
    }
}
