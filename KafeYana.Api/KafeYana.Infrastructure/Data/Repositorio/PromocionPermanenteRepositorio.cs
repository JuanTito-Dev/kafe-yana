using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
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

        public async Task<List<PromocionPermanente>> ObtenerActivasPorRecompensaAsync(string tipoRecompensa)
        {
            return await _db.PromocionPermanentes
                .AsNoTracking()
                .Include(p => p.ProductoCanjeable)
                .Where(p => p.Activo && p.TipoRecompensa == tipoRecompensa)
                .OrderByDescending(p => p.ValorRecompensa)
                .ThenBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<PromocionPermanente?> ObtenerActivaProductoGratisAsync(int idPromocionPermanente)
        {
            return await _db.PromocionPermanentes
                .Include(p => p.ProductoCanjeable)
                .FirstOrDefaultAsync(p =>
                    p.Id == idPromocionPermanente
                    && p.Activo
                    && p.TipoRecompensa == TipoRecompensaPromocion.ProductoGratis);
        }
    }
}
