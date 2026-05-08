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
    public class PedidoRepositorio : GenericRepositorio<Pedido>, IPedidoRepositorio
    {
        private readonly DbSet<Pedido> _dbSet;
        public PedidoRepositorio(AppDbContext _db) : base(_db)
        {
            _dbSet = _db.Set<Pedido>();
        }

        public async Task<Pedido?> TraerPedido(int Id)
        {
            return await _dbSet
                .AsSplitQuery()
                .Include(x => x.Rondas)
                    .ThenInclude(x => x.Detalle)
                .Include(x => x.Cliente)
                .FirstOrDefaultAsync(x => x.Id == Id);
        }
    }
}
