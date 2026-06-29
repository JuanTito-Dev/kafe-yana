using System.Linq;
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
    public class HistorialReferidoQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero, RolesKafe.Mesero })]
        public Task<OffsetPage<HistorialReferido>> HistorialReferidos(
            [Service] IHistorialReferidoRepositorio repo,
            int? skip,
            int? take,
            CancellationToken ct = default)
            => repo.GetHistorial()
                   .OrderByDescending(h => h.Fecha)
                   .ToOffsetPageAsync(skip, take, ct);
    }
}
