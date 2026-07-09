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
    public class RecetaQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Receta>> Recetas(
            [Service] IRecetaRepositorio _db,
            int? skip,
            int? take,
            int? id = null,
            bool? soloConElaborado = null,
            CancellationToken ct = default)
        {
            IQueryable<Receta> q = _db.GetRecetas();

            if (id.HasValue)
                q = q.Where(r => r.Id == id.Value);

            if (soloConElaborado == true)
                q = q.Where(r => r.Elaborado != null);

            return q.OrderBy(r => r.Nombre).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
