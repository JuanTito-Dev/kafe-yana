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
    public class InsumoQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Insumo>> Insumos(
            [Service] IInsumoRepositorio _db,
            int? skip,
            int? take,
            string? search = null,
            string? categoria = null,
            CancellationToken ct = default)
        {
            IQueryable<Insumo> q = _db.GetInsumos();

            if (!string.IsNullOrWhiteSpace(categoria))
                q = q.Where(i => i.Categoria == categoria);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(i => i.Nombre.ToLower().Contains(s)
                               || i.Categoria.ToLower().Contains(s));
            }

            return q.OrderBy(i => i.Nombre).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
