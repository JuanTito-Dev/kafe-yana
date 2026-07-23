using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class MesaQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Mesa>> Mesas(
            [Service] IMesaRepositorio _Mesa,
            int? skip,
            int? take,
            int? id = null,
            CancellationToken ct = default)
        {
            IQueryable<Mesa> q = _Mesa.MesaQuery();

            if (id.HasValue)
                q = q.Where(m => m.Id == id.Value);

            return q.OrderBy(m => m.Nombre).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
