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
    public class AjustesQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<Stock_Ajuste>> Ajustes(
            [Service] IAjusteStockRepositorio _db,
            int? skip,
            int? take,
            CancellationToken ct = default)
            => _db.Stock_AjusteQuery()
                  .AsNoTracking()
                  .OrderByDescending(a => a.Fecha)
                  .ToOffsetPageAsync(skip, take, ct);
    }
}
