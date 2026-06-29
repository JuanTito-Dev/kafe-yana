using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CodigoSiatQuery
    {
        [Authorize(Roles = new[]
        {
            RolesKafe.Admin,
            RolesKafe.Mesero,
            RolesKafe.Cajero,
            RolesKafe.Asistente
        })]
        public Task<OffsetPage<CodigoSiat>> CodigosSiat(
            [Service] ICodigoSiatRepositorio repository,
            int? skip,
            int? take,
            string? search = null,
            CancellationToken ct = default)
        {
            IQueryable<CodigoSiat> q = repository.Query();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(x => x.CodigoProducto.ToLower().Contains(s)
                               || x.DescripcionProducto.ToLower().Contains(s)
                               || x.DescripcionActividad.ToLower().Contains(s));
            }

            return q.OrderBy(x => x.CodigoProducto).ThenBy(x => x.CodigoActividad)
                    .ToOffsetPageAsync(skip, take, ct);
        }
    }
}
