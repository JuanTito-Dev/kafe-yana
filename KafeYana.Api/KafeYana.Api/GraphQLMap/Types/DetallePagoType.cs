using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetallePagoType : ObjectType<Detalle_Pago>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_Pago> descriptor)
        {
            descriptor.Field(d => d.Venta).Ignore();
        }
    }
}
