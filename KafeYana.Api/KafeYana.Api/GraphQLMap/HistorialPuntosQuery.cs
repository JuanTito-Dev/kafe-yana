using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class HistorialPuntosQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero })]
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<HistorialPuntos> HistorialPuntos([Service] IHistorialPuntosRepositorio _historial)
        {
            return _historial.Query();
        }
    }
}
