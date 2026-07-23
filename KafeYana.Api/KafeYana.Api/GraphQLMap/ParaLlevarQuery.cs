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
    public class ParaLlevarQuery
    {
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<ParaLlevar>> ParaLlevar(
            [Service] IParaLlevarRepositorio _db,
            int? skip,
            int? take,
            CancellationToken ct)
            => _db.ParaLlevarQuery().ToOffsetPageAsync(skip, take, ct);
    }
}