using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Queries
{
    [ExtendObjectType("Query")]
    public class ProductoQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Producto> Productos([Service] IProductoRepositorio _db)
        {
            return _db.GetProductos();
        }
    }
}
