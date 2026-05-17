using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IPromocionPermanenteRepositorio : IGenericRepositorio<PromocionPermanente>
    {
        IQueryable<PromocionPermanente> GetPromociones();
    }
}
