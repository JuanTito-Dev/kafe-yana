using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    /// <inheritdoc />
    public class CatPaisOrigenRepositorio : ICatPaisOrigenRepositorio
    {
        private readonly DbSet<CatPaisOrigen> _set;

        public CatPaisOrigenRepositorio(AppDbContext db)
        {
            _set = db.Set<CatPaisOrigen>();
        }

        public Task<CatPaisOrigen?> GetByCodigoAsync(int codigo) =>
            _set.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == codigo);

        public async Task<IReadOnlyList<CatPaisOrigen>> GetAllOrderedAsync() =>
            await _set.AsNoTracking().OrderBy(p => p.Codigo).ToListAsync();
    }
}