using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class PromocionTemporadaRepositorio(AppDbContext db)
        : GenericRepositorio<PromocionTemporada>(db), IPromocionTemporadaRepositorio
    {
        public IQueryable<PromocionTemporada> GetPromociones()
        {
            return _db.Set<PromocionTemporada>()
                .AsNoTracking()
                .Include(x => x.ProductosCanjeables)
                    .ThenInclude(x => x.ProductoCanjeable)
                .AsQueryable();
        }

        public Task<PromocionTemporada?> ObtenerConEnlacesTrackedAsync(int id)
        {
            return _db.Set<PromocionTemporada>()
                .Include(x => x.ProductosCanjeables)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
