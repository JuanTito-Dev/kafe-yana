using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class CajaHistorialRepositorio : GenericRepositorio<CajaHistorial>, ICajaHistorialRepositorio
    {
        private readonly DbSet<CajaHistorial> _set;
        public CajaHistorialRepositorio(AppDbContext _db) : base(_db)
        {
            _set = _db.Set<CajaHistorial>();
        }

        public async Task<int> ContarHistorial()
        {
            return await _set.CountAsync();
        }

        public IQueryable<CajaHistorial> QueryConMovimientos()
        {
            return _Set.AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Movimientos);
        }
    }
}
