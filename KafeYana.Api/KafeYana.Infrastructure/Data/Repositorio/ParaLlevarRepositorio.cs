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
    public class ParaLlevarRepositorio : GenericRepositorio<ParaLlevar>, IParaLlevarRepositorio
    {
        private readonly DbSet<ParaLlevar> _set;

        public ParaLlevarRepositorio(AppDbContext _db) : base(_db)
        {
            _set = _db.Set<ParaLlevar>();
        }

        public async Task<ParaLlevar?> GetParaLlevarConPedido()
        {
            return await _set
                .Include(x => x.Pedido)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<ParaLlevar?> GetPorPedidoActivoAsync(int idPedido)
        {
            return await _set
                .Include(x => x.Pedido)
                .FirstOrDefaultAsync(x => x.Id_Pedido == idPedido && !x.Disponible);
        }

        public async Task<bool> TienePedidoActivo()
        {
            return await _set.AnyAsync(x => x.Disponible == false);
        }

        public IQueryable<ParaLlevar> ParaLlevarQuery()
        {
            return _set
               .AsNoTracking()
               .Include(x => x.Pedido!)
                   .ThenInclude(p => p.Rondas)
                       .ThenInclude(r => r.Detalle)
                           .ThenInclude(d => d.Producto)
               .Include(x => x.Pedido!)
                   .ThenInclude(p => p.Rondas)
                       .ThenInclude(r => r.Detalle)
                           .ThenInclude(d => d.ItemsCombo)
               .AsQueryable();
        }
    }
}
