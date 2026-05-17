using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class AceleradorPuntosRepositorio(AppDbContext _db)
        : GenericRepositorio<AceleradorPuntos>(_db), IAceleradorPuntosRepositorio
    {
        public async Task<List<AceleradorPuntos>> ObtenerTodosAsync()
        {
            return await _db.AceleradorPuntos.OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<AceleradorPuntos?> ObtenerPorTipoAsync(string tipo)
        {
            return await _db.AceleradorPuntos.FirstOrDefaultAsync(x => x.Tipo == tipo);
        }
    }
}
