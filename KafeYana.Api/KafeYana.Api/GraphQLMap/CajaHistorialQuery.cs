using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CajaHistorialQuery 
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public IQueryable<CajaHistorial> CajaHistorial([Service] ICajaHistorialRepositorio _db)
        {
            return _db.Query().AsQueryable() ;
        }

        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        public IQueryable<CajaHistorialMovimiento> CajaHistorialMovimiento([Service] ICajaHistorialMovimientoRepositorio _db)
        {
            return _db.Query().AsQueryable();
        }
    }
}
