using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ParaLlevarQuery
    {
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]

        public IQueryable<ParaLlevar> ParaLlevar([Service] IParaLlevarRepositorio _db)
        {
            return _db.ParaLlevarQuery();
        }
    }
}
