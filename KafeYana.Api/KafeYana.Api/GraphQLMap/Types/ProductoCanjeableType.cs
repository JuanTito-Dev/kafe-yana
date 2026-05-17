using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ProductoCanjeableType : ObjectType<ProductoCanjeable>
    {
        protected override void Configure(IObjectTypeDescriptor<ProductoCanjeable> descriptor)
        {
            descriptor.Field(x => x.Producto).Ignore();
        }
    }
}
