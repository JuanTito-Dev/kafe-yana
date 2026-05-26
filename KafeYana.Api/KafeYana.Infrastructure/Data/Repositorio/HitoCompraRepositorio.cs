using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HitoCompraRepositorio(AppDbContext db)
        : GenericRepositorio<HitoCompra>(db), IHitoCompraRepositorio
    {
        public IQueryable<HitoCompra> GetHitos()
        {
            return _db.HitosCompra
                .AsNoTracking()
                .Include(x => x.ProductoCanjeable)
                .AsQueryable();
        }

        public Task<HitoCompra?> ObtenerActivoParaReclamoAsync(int id)
        {
            return _db.HitosCompra
                .Include(x => x.ProductoCanjeable)
                .FirstOrDefaultAsync(x => x.Id == id && x.Activo);
        }
    }
}
