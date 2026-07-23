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
    public class PromocionTemporadaQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero, RolesKafe.Mesero })]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Task<OffsetPage<PromocionTemporada>> PromocionTemporadas(
            [Service] IPromocionTemporadaRepositorio _repo,
            int? skip,
            int? take,
            CancellationToken ct)
            => _repo.GetPromociones().ToOffsetPageAsync(skip, take, ct);
    }
}