using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ReglaBasePuntosRepositorio(AppDbContext _db)
        : GenericRepositorio<ReglaBasePuntos>(_db), IReglaBasePuntosRepositorio
    {
        public async Task<ReglaBasePuntos?> ObtenerReglaAsync()
        {
            return await _db.ReglaBasePuntos.FirstOrDefaultAsync();
        }
    }
}
