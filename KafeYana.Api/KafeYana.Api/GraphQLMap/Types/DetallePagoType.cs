using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetallePagoType : ObjectType<Detalle_Pago>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_Pago> descriptor)
        {
            descriptor.Field(d => d.Venta).Ignore();

            // Cantidad ya devuelta en notas de ajuste válidas (estado SIAT = Validada).
            // Se calcula agregada a partir de NotaAjusteDetalle filtrando por la venta
            // padre del detalle. Se usa en el frontend para deshabilitar productos
            // 100% devueltos y para acotar la cantidad del input del modal.
            //
            // N+1 aceptable en v1: una query por detalle. Si el volumen crece, mover
            // a un BatchDataLoader por ventaId (ver plan).
            descriptor.Field("cantidadDevuelta")
                .Type<DecimalType>()
                .Resolve(async ctx =>
                {
                    var detalle = ctx.Parent<Detalle_Pago>();
                    var repo = ctx.Service<INotaAjusteRepositorio>();
                    var map = await repo.ObtenerCantidadDevueltaPorDetallePagoAsync(detalle.Id_venta);
                    return map.GetValueOrDefault(detalle.Id, 0m);
                });
        }
    }
}
