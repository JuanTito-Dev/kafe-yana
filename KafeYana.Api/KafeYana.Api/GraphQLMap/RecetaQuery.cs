using HotChocolate.Authorization;
using KafeYana.Application.Dtos.RecetaDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class RecetaQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]

        public IQueryable<Receta> Recetas([Service] IRecetaRepositorio _db)
        {
            return _db.GetRecetas();
        }
    }
}
