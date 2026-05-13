using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CajaHistorialType : ObjectType<CajaHistorial>
    {
        protected override void Configure(IObjectTypeDescriptor<CajaHistorial> descriptor)
        {
            descriptor.Field(x => x.Movimientos).Type<ListType<CajaHistorialMovimientoType>>();
            descriptor.Field(x => x.TotalEfectivo).Type<DecimalType>().Description("Total en efectivo al cierre");
            descriptor.Field(x => x.TotalTarjeta).Type<DecimalType>().Description("Total con tarjeta al cierre");
            descriptor.Field(x => x.TotalQr).Type<DecimalType>().Description("Total por QR al cierre");
        }
    }
}
