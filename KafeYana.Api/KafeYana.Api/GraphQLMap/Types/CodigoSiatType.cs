using KafeYana.Domain.Entities.Facturacion;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CodigoSiatType : ObjectType<CodigoSiat>
    {
        protected override void Configure(IObjectTypeDescriptor<CodigoSiat> descriptor)
        {
            descriptor.Description("Catálogo de códigos de producto y actividad económica del SIAT.");
        }
    }
}
