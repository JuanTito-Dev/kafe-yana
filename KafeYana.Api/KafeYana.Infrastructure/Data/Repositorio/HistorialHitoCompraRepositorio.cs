using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HistorialHitoCompraRepositorio(AppDbContext db)
        : GenericRepositorio<HistorialHitoCompra>(db), IHistorialHitoCompraRepositorio
    {
        public Task<bool> ExisteReclamoAsync(int idCliente, int idHitoCompra)
        {
            return _db.Set<HistorialHitoCompra>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id_Cliente == idCliente
                    && x.Id_HitoCompra == idHitoCompra);
        }

        public Task<List<HistorialHitoCompra>> ObtenerReclamadosPorClienteAsync(int idCliente)
        {
            return _db.Set<HistorialHitoCompra>()
                .AsNoTracking()
                .Include(x => x.HitoCompra)
                    .ThenInclude(x => x!.ProductoCanjeable)
                .Where(x => x.Id_Cliente == idCliente)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
        }
    }
}
