using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class InsumoMovimientosQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        public IQueryable<InsumoMovimiento> InsumoMovimientos(int Id, [Service] IInsumoMovimientoRepositorio _db)
        {
            return _db.Query().Where(x => x.Id_insumo == Id);
        }
    }
}
