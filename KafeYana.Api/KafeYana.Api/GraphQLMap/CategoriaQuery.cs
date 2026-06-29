using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CategoriaQuery
    {
        /// <summary>
        /// Lista de categorías con paginación offset, ordenadas por nombre.
        /// El parámetro soloConProductos permite filtrar solo las que tienen al menos un producto.
        /// </summary>
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Categoria>> Categorias(
            [Service] ICategoriaRepositorio _db,
            int? skip,
            int? take,
            bool? soloConProductos = null,
            CancellationToken ct = default)
        {
            IQueryable<Categoria> q = _db.QueryCategorias().AsNoTracking();

            if (soloConProductos == true)
                q = q.Where(c => c.Productos.Any());

            return q.OrderBy(c => c.Nombre)
                    .ToOffsetPageAsync(skip, take, ct);
        }
    }
}
