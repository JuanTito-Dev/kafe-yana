using KafeYana.Application.Auxiliares.Recetas;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class RecetaType : ObjectType<Receta>
    {
        protected override void Configure(IObjectTypeDescriptor<Receta> descriptor)
        {
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Nombre);
            descriptor.Field(x => x.Detalles);
            descriptor.Field(x => x.Id_Elaborado).Ignore();
            descriptor.Field("cantidadProducible")
                .Type<DecimalType>()
                .Resolve(async ctx =>
                {
                    var receta = ctx.Parent<Receta>();
                    var db = ctx.Service<AppDbContext>();

                    var detalles = await db.DetalleReceta
                        .Where(d => d.Id_receta == receta.Id)
                        .Include(x => x.Insumo)
                        .ToListAsync();

                    if (!detalles.Any()) return 0;

                    return detalles
                        .Select(d =>
                        {
                            if (d.Insumo == null || d.Cantidad == 0)
                                return 0;

                            return (int)Math.Floor(
                                (double)d.Insumo.Stock_actual /
                                ((double)d.Cantidad * (1 + (double)d.Merma / 100.0))
                            );
                        })
                        .Min();
                });

            descriptor.Field(x => x.Detalles).Type<ListType<DetalleType>>();
        }
    }
}
