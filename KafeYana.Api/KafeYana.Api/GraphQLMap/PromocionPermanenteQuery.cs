using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class PromocionPermanenteQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero, RolesKafe.Mesero })]
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<PromocionPermanente> PromocionPermanentes([Service] IPromocionPermanenteRepositorio _repo)
        {
            return _repo.GetPromociones();
        }
    }
}
