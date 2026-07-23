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

        /// <summary>
        /// Borra el pedido completo — solo válido para un cobro de 100% en un solo
        /// paso (sin pasar por sub-venta). Si el pedido ya tiene sub-ventas
        /// registradas, la FK Restrict (ver <c>SubVentaConfig</c>) hace fallar este
        /// borrado: ese historial de cobros/facturas nunca debe destruirse
        /// silenciosamente. <see cref="VentaServices.ProcesarVenta"/> valida esto
        /// explícitamente antes de llegar acá.
        /// </summary>
        public async Task EliminarConAbonosAsync(Pedido pedido)
        {
            await Remove(pedido);
        }

        public async Task<Pedido?> TraerPedido(int Id)
        {
            return await _dbSet
                .AsSplitQuery()
                .Include(x => x.Rondas)
                    .ThenInclude(x => x.Detalle)
                        .ThenInclude(d => d.Producto)
                            .ThenInclude(p => p.Comprado)
                .Include(x => x.Rondas)
                    .ThenInclude(x => x.Detalle)
                        .ThenInclude(d => d.Producto)
                            .ThenInclude(p => p.Elaborado)
                .Include(x => x.Rondas)
                    .ThenInclude(x => x.Detalle)
                        .ThenInclude(d => d.ItemsCombo)
                .Include(x => x.Cliente)
                .Include(x => x.SubVentas)
                .FirstOrDefaultAsync(x => x.Id == Id);
        }
    }
}
