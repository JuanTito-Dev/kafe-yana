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
    public class ElaboradoQuery
    {
        /// <summary>
        /// Lista de elaborados con paginación offset y búsqueda opcional.
        /// El filtrado y el orden se aplican directamente sobre el IQueryable antes de paginar.
        /// Se cargan eager las navegaciones necesarias para las páginas de inventario y POS:
        ///   - Producto + Categoria
        ///   - Receta + Detalles + Insumo  (para KPIs y modal de receta)
        ///   - Variaciones + Opciones       (para el catálogo de POS)
        /// </summary>
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Elaborado>> elaborados(
            [Service] IElaboradoRepositorio _db,
            int? skip,
            int? take,
            string? search = null,
            bool? producible = null,
            int? idProducto = null,
            CancellationToken ct = default)
        {
            IQueryable<Elaborado> q = _db.QueryElaborados()
                .AsNoTracking()
                .Include(e => e.Producto!)
                    .ThenInclude(p => p.Categoria)
                .Include(e => e.Receta!)
                    .ThenInclude(r => r.Detalles)
                        .ThenInclude(d => d.Insumo)
                .Include(e => e.Variaciones!)
                    .ThenInclude(v => v.Opciones);

            if (idProducto.HasValue)
                q = q.Where(e => e.Id_Producto == idProducto.Value);

            if (producible.HasValue)
                q = q.Where(e => e.Producible == producible.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(e => e.Producto!.Nombre.ToLower().Contains(s)
                               || (e.Producto.Categoria != null && e.Producto.Categoria.Nombre.ToLower().Contains(s)));
            }

            return q.OrderBy(e => e.Producto!.Nombre)
                    .ToOffsetPageAsync(skip, take, ct);
        }
    }
}
