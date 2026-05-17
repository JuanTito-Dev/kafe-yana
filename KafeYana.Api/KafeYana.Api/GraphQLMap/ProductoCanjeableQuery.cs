using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ProductoCanjeableQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero, RolesKafe.Mesero })]
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProductoCanjeable> ProductosCanjeables([Service] IProductoCanjeableRepositorio _repo)
        {
            return _repo.GetCanjeables();
        }
    }
}
