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
    public class OrdenCompraQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<OrdenCompra>> Ordenes(
            [Service] IOrdenCompraRepositorio _db,
            int? skip,
            int? take,
            string? search = null,
            CancellationToken ct = default)
        {
            IQueryable<OrdenCompra> q = _db.Query();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(o => o.Codigo.ToLower().Contains(s)
                               || o.Nombre_Proveedor.ToLower().Contains(s));
            }

            return q.OrderByDescending(o => o.Fecha).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
