using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Queries
{
    [ExtendObjectType("Query")]
    public class ProveedorQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Proveedor> proveedor([Service] IProveedorRepositorio _db)
        {
            return _db.ProveedorQuery();
        }
    }
}
