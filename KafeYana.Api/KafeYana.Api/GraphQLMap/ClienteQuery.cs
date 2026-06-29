using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Api.Helpers;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ClienteQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero, RolesKafe.Asistente })]
        public Task<OffsetPage<Cliente>> Clientes(
            [Service] IClienteRespositorio _clientes,
            int? skip,
            int? take,
            string? search = null,
            int? id = null,
            int? dni = null,
            CancellationToken ct = default)
        {
            IQueryable<Cliente> q = _clientes.GetClientes().AsNoTracking();

            if (id.HasValue)
                q = q.Where(c => c.Id == id.Value);

            if (dni.HasValue)
                q = q.Where(c => c.Dni == dni.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(c => c.Nombre.ToLower().Contains(s)
                               || (c.Celular != null && c.Celular.ToLower().Contains(s)));
            }

            return q.OrderBy(c => c.Nombre).ToOffsetPageAsync(skip, take, ct);
        }
    }
}
