using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class PromocionPermanenteRepositorio(AppDbContext _db)
        : GenericRepositorio<PromocionPermanente>(_db), IPromocionPermanenteRepositorio
    {
        public IQueryable<PromocionPermanente> GetPromociones()
        {
            return _db.PromocionPermanentes.AsNoTracking().AsQueryable();
        }
    }
}
