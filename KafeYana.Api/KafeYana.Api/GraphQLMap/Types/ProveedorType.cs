using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ProveedorType : ObjectType<Proveedor>
    {
        protected override void Configure(IObjectTypeDescriptor<Proveedor> descriptor)
        {
            descriptor
                .Field(x => x.Id)
                .Type<NonNullType<IntType>>();

            descriptor
                .Field(x => x.Razon_Social)
                .Type<NonNullType<StringType>>();
        }
    }
}