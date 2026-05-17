using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class PromocionPermanenteType : ObjectType<PromocionPermanente>
    {
        protected override void Configure(IObjectTypeDescriptor<PromocionPermanente> descriptor)
        {
            descriptor.Field(x => x.ProductoCanjeable).Ignore();
        }
    }
}
