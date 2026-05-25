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

        public async Task<List<PromocionTemporada>> ObtenerActivasVigentesAsync(DateTime fechaReferencia)
        {
            var fecha = fechaReferencia.Date;

            return await _db.Set<PromocionTemporada>()
                .AsNoTracking()
                .Include(x => x.ProductosCanjeables)
                    .ThenInclude(x => x.ProductoCanjeable)
                .Where(x =>
                    x.Activo
                    && x.FechaInicio.Date <= fecha
                    && x.FechaFin.Date >= fecha)
                .OrderBy(x => x.FechaFin)
                .ThenBy(x => x.Nombre)
                .ToListAsync();
        }

        public Task<PromocionTemporada?> ObtenerActivaVigenteParaReclamoAsync(int id, DateTime fechaReferencia)
        {
            var fecha = fechaReferencia.Date;

            return _db.Set<PromocionTemporada>()
                .Include(x => x.ProductosCanjeables)
                    .ThenInclude(x => x.ProductoCanjeable)
                .FirstOrDefaultAsync(x =>
                    x.Id == id
                    && x.Activo
                    && x.FechaInicio.Date <= fecha
                    && x.FechaFin.Date >= fecha);
        }
    }
}
