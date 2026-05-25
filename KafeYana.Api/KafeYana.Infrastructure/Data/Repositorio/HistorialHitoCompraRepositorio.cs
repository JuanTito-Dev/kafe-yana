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
    }
}
