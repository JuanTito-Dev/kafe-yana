using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class OrdenCompraQuery 
    {
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]

        public IQueryable<OrdenCompra> Ordenes([Service] IOrdenCompraRepositorio _db) 
        {
            return _db.Query().AsQueryable();
        }

    }
}
