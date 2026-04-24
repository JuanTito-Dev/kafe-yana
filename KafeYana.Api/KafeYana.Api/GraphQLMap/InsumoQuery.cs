using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class InsumoQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        public IQueryable<Insumo> Insumos([Service] IInsumoRepositorio _db)
        {
            return _db.GetInsumos();
        }
    }
}
