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
    public class HistorialPuntosQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero })]
        public Task<OffsetPage<HistorialPuntos>> HistorialPuntos(
            [Service] IHistorialPuntosRepositorio _historial,
            int? skip,
            int? take,
            int? idCliente = null,
            CancellationToken ct = default)
        {
            IQueryable<HistorialPuntos> q = _historial.Query();

            if (idCliente.HasValue)
                q = q.Where(h => h.Id_Cliente == idCliente.Value);

            return q.OrderByDescending(h => h.Fecha).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
