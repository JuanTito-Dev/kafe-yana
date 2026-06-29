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
    public class InsumoMovimientosQuery
    {
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<InsumoMovimiento>> InsumoMovimientos(
            int Id,
            [Service] IInsumoMovimientoRepositorio _db,
            int? skip,
            int? take,
            CancellationToken ct)
            => _db.Query().Where(x => x.Id_insumo == Id).ToOffsetPageAsync(skip, take, ct);
    }
}