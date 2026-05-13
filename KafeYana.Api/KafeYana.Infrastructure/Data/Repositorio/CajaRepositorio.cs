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
    public class CajaRepositorio : GenericRepositorio<Caja>, ICajaRepositorio
    {
        private readonly DbSet<Caja> _set;
        public CajaRepositorio(AppDbContext _db) : base(_db)
        {
            _set = _db.Set<Caja>();
        }

        public async Task<bool> ExisteCaja()
        {
            return await _set.AnyAsync();
        }

        public async Task<Caja?> ObtenerCaja()
        {
            return await _set.FirstOrDefaultAsync();
        }

        public async Task<Caja?> ObtenerCajaConMovimientos()
        {
            return await _set
                .Include(x => x.Movimientos)
                .FirstOrDefaultAsync();
        }
    }
}
