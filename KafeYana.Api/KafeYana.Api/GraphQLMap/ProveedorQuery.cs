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
    public class ProveedorQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Proveedor>> Proveedores(
            [Service] IProveedorRepositorio _repository,
            int? skip,
            int? take,
            int? id = null,
            CancellationToken ct = default)
        {
            IQueryable<Proveedor> q = _repository.Query();

            if (id.HasValue)
                q = q.Where(p => p.Id == id.Value);

            return q.OrderBy(p => p.Razon_Social).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
