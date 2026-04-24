using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class RondaRepositorio : GenericRepositorio<Ronda>, IRondaRepositorio
    {
        private readonly DbSet<Ronda> _Rondas;
        public RondaRepositorio(AppDbContext _db) : base(_db)
        {
            _Rondas = _db.Set<Ronda>();
        }

        public async Task<int> Count(Expression<Func<Ronda, bool>> filtro)
        {
            return await _Rondas.CountAsync(filtro);
        }
    }
}
