using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class HistorialPuntosRepositorio(AppDbContext _db)
        : GenericRepositorio<HistorialPuntos>(_db), IHistorialPuntosRepositorio
    {
        public async Task<List<HistorialPuntos>> ObtenerPorClienteAsync(int idCliente)
        {
            return await _db.HistorialPuntos
                .Where(x => x.Id_Cliente == idCliente)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
        }
    }
}
