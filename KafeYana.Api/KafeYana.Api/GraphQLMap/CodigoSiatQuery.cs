using HotChocolate.Authorization;
using KafeYana.Api.GraphQLMap.Types;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CodigoSiatQuery
    {
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[]
        {
            RolesKafe.Admin,
            RolesKafe.Mesero,
            RolesKafe.Cajero,
            RolesKafe.Asistente
        })]
        [GraphQLType(typeof(ListType<CodigoSiatType>))]
        public IQueryable<CodigoSiat> CodigosSiat([Service] ICodigoSiatRepositorio repository)
        {
            return repository.Query().OrderBy(x => x.CodigoProducto).ThenBy(x => x.CodigoActividad);
        }
    }
}
