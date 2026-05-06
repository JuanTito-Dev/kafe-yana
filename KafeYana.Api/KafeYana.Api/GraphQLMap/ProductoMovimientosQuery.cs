using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ProductoMovimientosQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        public IQueryable<ProductoMovimiento> MovimientoProducto([Service] IProductoMovimientoRepositorio _db, int Id)
        {
            return _db.Query().Where(x => x.Id_Producto == Id);
        }

    }
}
