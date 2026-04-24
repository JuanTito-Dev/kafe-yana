using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ClienteType : ObjectType<Cliente>
    {
        protected override void Configure(IObjectTypeDescriptor<Cliente> descriptor)
        {
            descriptor.Field(X => X.Correonormalizado).Ignore();
        }
    }
}
