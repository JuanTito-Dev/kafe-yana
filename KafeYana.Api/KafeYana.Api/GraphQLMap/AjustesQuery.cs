using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{

    [ExtendObjectType("Query")]
    public class AjustesQuery 
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        public IQueryable<Stock_Ajuste> Ajustes([Service]IAjusteStockRepositorio _db)
        {
            return _db.Stock_AjusteQuery().AsNoTracking();
        }
    }
}
