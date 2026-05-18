using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ReferidosConfigRepositorio(AppDbContext db)
        : GenericRepositorio<ReferidosConfig>(db), IReferidosConfigRepositorio
    {
        public Task<ReferidosConfig?> ObtenerUnicaAsync()
        {
            return _db.ReferidosConfigs.OrderBy(x => x.Id).FirstOrDefaultAsync();
        }
    }
}
