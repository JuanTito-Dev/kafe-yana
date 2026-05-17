using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CajaQuery
    {
        [UseProjection]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public async Task<Caja?> Caja([Service] ICajaRepositorio _db)
        {
            return await _db.Query().FirstOrDefaultAsync();
        }

        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public IQueryable<CajaMovimiento> CajaMoviminetos(ICajaMovimientoRepositorio _db)
        {
            return _db.Query().AsQueryable();
        }
    }
}
