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
    public class ProductoMovimientosQuery
    {
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public Task<OffsetPage<ProductoMovimiento>> MovimientoProducto(
            [Service] IProductoMovimientoRepositorio _db,
            int Id,
            int? skip,
            int? take,
            CancellationToken ct)
            => _db.Query().Where(x => x.Id_Producto == Id).ToOffsetPageAsync(skip, take, ct);
    }
}