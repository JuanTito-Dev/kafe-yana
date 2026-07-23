using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ComboQuery
    {
        /// <summary>
        /// Lista de combos (promociones) con paginación offset y búsqueda opcional.
        /// El filtrado y el orden se aplican directamente sobre el IQueryable antes de paginar.
        /// </summary>
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Promocion>> Combos(
            [Service] IComboRepositorio _db,
            int? skip,
            int? take,
            string? search = null,
            int? idProducto = null,
            CancellationToken ct = default)
        {
            IQueryable<Promocion> q = _db.GetCombos()
                .AsNoTracking()
                .Include(p => p.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto);

            if (idProducto.HasValue)
                q = q.Where(p => p.Producto_Id == idProducto.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(p => p.Producto!.Nombre.ToLower().Contains(s));
            }

            return q.OrderBy(p => p.Producto!.Nombre)
                    .ToOffsetPageAsync(skip, take, ct);
        }
    }
}
