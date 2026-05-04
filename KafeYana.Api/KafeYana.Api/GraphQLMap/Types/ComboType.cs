using KafeYana.Api.DataLoaders;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ComboType : ObjectType<Promocion>
    {
        protected override void Configure(IObjectTypeDescriptor<Promocion> descriptor)
        {
            descriptor.Field(x => x.Producto).Type<ProductoType>();
            descriptor.Field(x => x.Id).IsProjected(true); ;      // interno, no visible en schema
            descriptor.Field(x => x.Producto_Id).IsProjected(true); // interno, no visible en schema
            descriptor.Field(x => x.Detalles).Type<ListType<PromocionDetalleType>>();
            descriptor.Field("cantidadProducible")
                .Type<IntType>()
                .Resolve(async ctx =>
                {
                    var promocion = ctx.Parent<Promocion>();
                    var dataLoader = ctx.DataLoader<PromocionCantidadProducibleDataLoader>();
                    return await dataLoader.LoadAsync(promocion.Id);
                });
        }
    }
}
