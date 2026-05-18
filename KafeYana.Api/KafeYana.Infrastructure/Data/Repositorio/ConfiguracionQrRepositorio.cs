using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ConfiguracionQrRepositorio : GenericRepositorio<ConfiguracionQr>, IConfiguracionQrRepositorio
    {
        private readonly DbSet<ConfiguracionQr> _set;

        public ConfiguracionQrRepositorio(AppDbContext db) : base(db)
        {
            _set = db.Set<ConfiguracionQr>();
        }

        public Task<ConfiguracionQr?> ObtenerUnicaAsync() =>
            _set.OrderBy(x => x.Id).FirstOrDefaultAsync();

        public Task<bool> ExisteAlgunaAsync() =>
            _set.AnyAsync();
    }
}
