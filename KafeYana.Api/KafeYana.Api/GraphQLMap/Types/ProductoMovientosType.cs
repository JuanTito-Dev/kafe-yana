using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ProductoMovientosType : ObjectType<ProductoMovimiento>
    {
        protected override void Configure(IObjectTypeDescriptor<ProductoMovimiento> descriptor)
        {
            descriptor.Field(x => x.Producto).Ignore();
            descriptor.Field(x => x.Fecha);
            descriptor.Field(x => x.Tipo)
                .Resolve(ctx => ctx.Parent<ProductoMovimiento>().Tipo);

            descriptor.Field(x => x.Referencia)
                .Resolve(ctx => ctx.Parent<ProductoMovimiento>().Referencia);
            descriptor.Field(x => x.Cantidad);
            descriptor.Field(x => x.Costo_Unitario);
            descriptor.Field(x => x.Total);
            descriptor.Field(x => x.Stock_resultante);
        }
    }
}
