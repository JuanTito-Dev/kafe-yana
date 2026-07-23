using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ProductoCanjeableQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero, RolesKafe.Mesero })]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Task<OffsetPage<ProductoCanjeable>> ProductosCanjeables(
            [Service] IProductoCanjeableRepositorio _repo,
            int? skip,
            int? take,
            CancellationToken ct)
            => _repo.GetCanjeables().ToOffsetPageAsync(skip, take, ct);
    }
}