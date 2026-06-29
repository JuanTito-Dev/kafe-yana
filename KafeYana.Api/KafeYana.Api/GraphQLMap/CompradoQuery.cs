using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CompradoQuery
    {
        /// <summary>
        /// Lista de productos comprados con paginación offset, búsqueda y filtro por categoría.
        /// El filtrado y el orden se aplican directamente sobre el IQueryable antes de paginar,
        /// evitando el problema de HotChocolate v15 donde [UseFiltering]/[UseSorting] generan
        /// tipos para OffsetPage&lt;T&gt; en lugar de para T.
        /// </summary>
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Comprado>> comprados(
            [Service] IProductoRepositorio _db,
            int? skip,
            int? take,
            string? search = null,
            string? categoria = null,
            int? idProducto = null,
            CancellationToken ct = default)
        {
            IQueryable<Comprado> q = _db.GetComprados()
                .AsNoTracking()
                .Include(c => c.Producto!)
                    .ThenInclude(p => p.Categoria);

            if (idProducto.HasValue)
                q = q.Where(c => c.Id_Producto == idProducto.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(c => c.Producto!.Nombre.ToLower().Contains(s)
                               || c.Producto.Descripcion.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
                q = q.Where(c => c.Producto!.Categoria!.Nombre == categoria);

            return q.OrderBy(c => c.Producto!.Nombre)
                    .ToOffsetPageAsync(skip, take, ct);
        }
    }
}
