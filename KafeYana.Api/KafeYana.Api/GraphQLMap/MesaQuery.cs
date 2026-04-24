using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class MesaQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin })]
        public IQueryable<Mesa> Mesas([Service] IMesaRepositorio _Mesa)
        {
            return _Mesa.MesaQuery();
        }
    }
}
