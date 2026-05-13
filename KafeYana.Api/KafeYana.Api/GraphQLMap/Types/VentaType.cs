using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class VentaType : ObjectType<Venta>
    {
        protected override void Configure(IObjectTypeDescriptor<Venta> descriptor)
        {
            descriptor.Field(v => v.Detalles).Type<ListType<DetalleVentaType>>()
                .Name("detalles")
                .Description("Lista de detalles de la venta");

            descriptor.Field(v => v.PagoEfectivo).Type<DecimalType>().Description("Monto pagado en efectivo");
            descriptor.Field(v => v.PagoTarjeta).Type<DecimalType>().Description("Monto pagado con tarjeta");
            descriptor.Field(v => v.PagoQr).Type<DecimalType>().Description("Monto pagado por QR");
        }
    }
}
