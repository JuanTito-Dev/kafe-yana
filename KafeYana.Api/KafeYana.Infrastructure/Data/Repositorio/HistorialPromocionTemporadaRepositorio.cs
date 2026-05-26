using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HistorialPromocionTemporadaRepositorio(AppDbContext db)
        : GenericRepositorio<HistorialPromocionTemporada>(db), IHistorialPromocionTemporadaRepositorio
    {
        public async Task<HashSet<int>> ObtenerIdsPromocionesReclamadasAsync(int idCliente)
        {
            var ids = await _db.Set<HistorialPromocionTemporada>()
                .AsNoTracking()
                .Where(x => x.Id_Cliente == idCliente)
                .Select(x => x.Id_PromocionTemporada)
                .ToListAsync();

            return ids.ToHashSet();
        }

        public Task<bool> ExisteReclamoAsync(int idCliente, int idPromocionTemporada)
        {
            return _db.Set<HistorialPromocionTemporada>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id_Cliente == idCliente
                    && x.Id_PromocionTemporada == idPromocionTemporada);
        }
    }
}
