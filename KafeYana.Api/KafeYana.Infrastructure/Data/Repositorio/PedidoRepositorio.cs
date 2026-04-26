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
            var pedido = _dbSet.AsSplitQuery()
                .Include(x => x.Rondas).ThenInclude(x => x.Detalle).ThenInclude(x => x.Opciones).ThenInclude(x => x.Opcion)
                .Include(x => x.Rondas).ThenInclude(x => x.Detalle).ThenInclude(x => x.producto)
                .Include(x => x.Cliente).AsQueryable();

            return await pedido.FirstOrDefaultAsync(x => x.Id == Id);
        }
    }
}
