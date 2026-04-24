using HotChocolate.Authorization;
using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.Dtos.ProductoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CompradoQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        public IQueryable<Comprado> comprados([Service] IProductoRepositorio _db)
        {
            return _db.GetComprados();
        }  
    }
}
