using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class SubVentaRepositorio : GenericRepositorio<SubVenta>, ISubVentaRepositorio
    {
        private readonly DbSet<SubVenta> _subVentas;

        public SubVentaRepositorio(AppDbContext db) : base(db)
        {
            _subVentas = db.Set<SubVenta>();
        }

        public async Task<SubVenta?> GetByIdConDetallesAsync(int id) =>
            await _subVentas
                .Include(x => x.Detalles)
                .Include(x => x.Pedido)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<List<SubVenta>> GetPendientesFacturarAsync(int? idMesa = null, int? idParaLlevar = null)
        {
            var query = _subVentas
                .Include(x => x.Detalles)
                .Include(x => x.Pedido).ThenInclude(p => p!.Mesa)
                .Include(x => x.Pedido).ThenInclude(p => p!.ParaLlevar)
                .Where(x => !x.Facturada);

            if (idMesa.HasValue)
                query = query.Where(x => x.Pedido != null && x.Pedido.Mesa != null && x.Pedido.Mesa.Id == idMesa.Value);

            if (idParaLlevar.HasValue)
                query = query.Where(x => x.Pedido != null && x.Pedido.ParaLlevar != null && x.Pedido.ParaLlevar.Id == idParaLlevar.Value);

            return await query.OrderByDescending(x => x.Fecha).ToListAsync();
        }

        public async Task<List<SubVenta>> GetByPedidoAsync(int idPedido) =>
            await _subVentas
                .Include(x => x.Detalles)
                .Include(x => x.Pedido).ThenInclude(p => p!.Mesa)
                .Include(x => x.Pedido).ThenInclude(p => p!.ParaLlevar)
                .Where(x => x.Id_Pedido == idPedido)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
    }
}
