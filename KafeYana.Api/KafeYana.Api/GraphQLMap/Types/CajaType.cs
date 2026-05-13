using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CajaType : ObjectType<Caja>
    {
        protected override void Configure(IObjectTypeDescriptor<Caja> descriptor)
        {
            descriptor.Field(x => x.Movimientos).Ignore();
            descriptor.Field(x => x.TotalEfectivo).Type<DecimalType>().Description("Total recaudado en efectivo");
            descriptor.Field(x => x.TotalTarjeta).Type<DecimalType>().Description("Total recaudado con tarjeta");
            descriptor.Field(x => x.TotalQr).Type<DecimalType>().Description("Total recaudado por QR");
        }
    }
}
