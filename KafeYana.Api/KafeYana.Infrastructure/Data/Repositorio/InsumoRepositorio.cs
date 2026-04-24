using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class InsumoRepositorio : GenericRepositorio<Insumo>, IInsumoRepositorio
    {
        private readonly DbSet<Insumo> _Insumos;
        public InsumoRepositorio(AppDbContext _db) :  base(_db)
        {
            _Insumos = _db.Set<Insumo>();
        }

        public IQueryable<Insumo> GetInsumos()
        {
            return _Insumos.AsNoTracking().AsQueryable();
        }
    }
}
