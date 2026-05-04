using KafeYana.Api.DataLoaders;
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
                .Type<IntType>()
                .Resolve(async ctx =>
                {
                    var receta = ctx.Parent<Receta>();
                    var dataLoader = ctx.DataLoader<RecetaCantidadProducibleDataLoader>();
                    return await dataLoader.LoadAsync(receta.Id);
                });

            descriptor.Field(x => x.Detalles).Type<ListType<DetalleType>>();
        }
    }
}
