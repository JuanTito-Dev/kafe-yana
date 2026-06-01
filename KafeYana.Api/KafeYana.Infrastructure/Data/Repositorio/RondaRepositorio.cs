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

        public async Task<Ronda?> TraerConDetallesAsync(int idRonda) =>
            await _Rondas
                .Include(x => x.Detalle)
                    .ThenInclude(d => d.Opciones)
                .Include(x => x.Detalle)
                    .ThenInclude(d => d.ItemsCombo)
                .Include(x => x.Detalle)
                    .ThenInclude(d => d.CompromisoInventario!)
                        .ThenInclude(c => c.Lineas)
                .FirstOrDefaultAsync(x => x.Id == idRonda);

        public async Task<decimal> SumSubTotalPorPedidoAsync(int idPedido)
        {
            var tracked = _db.ChangeTracker.Entries<Ronda>()
                .Where(e => e.Entity.Id_Pedido == idPedido)
                .ToList();

            var trackedIds = tracked.Select(e => e.Entity.Id).ToHashSet();

            var trackedSum = tracked
                .Where(e => e.State != EntityState.Deleted)
                .Sum(e => e.Entity.SubTotal);

            decimal dbSum = trackedIds.Count > 0
                ? await _Rondas.Where(r => r.Id_Pedido == idPedido && !trackedIds.Contains(r.Id)).SumAsync(r => r.SubTotal)
                : await _Rondas.Where(r => r.Id_Pedido == idPedido).SumAsync(r => r.SubTotal);

            return trackedSum + dbSum;
        }
    }
}
