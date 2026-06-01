using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio;

public class PedidoInventarioCompromisoRepositorio(AppDbContext db) : IPedidoInventarioCompromisoRepositorio
{
    private readonly DbSet<PedidoInventarioComprometido> _compromisos = db.Set<PedidoInventarioComprometido>();

    public async Task<PedidoInventarioComprometido?> ObtenerPorDetalleAsync(int idDetalleRonda) =>
        await _compromisos
            .Include(x => x.Lineas)
            .FirstOrDefaultAsync(x => x.Id_Detalle_Ronda == idDetalleRonda);

    public async Task<List<PedidoInventarioComprometido>> ObtenerPorPedidoAsync(int idPedido) =>
        await _compromisos
            .Include(x => x.Lineas)
            .Where(x => x.Id_Pedido == idPedido)
            .ToListAsync();

    public async Task CrearAsync(PedidoInventarioComprometido compromiso) =>
        await _compromisos.AddAsync(compromiso);

    public void Eliminar(PedidoInventarioComprometido compromiso) =>
        _compromisos.Remove(compromiso);

    public void EliminarRango(IEnumerable<PedidoInventarioComprometido> compromisos) =>
        _compromisos.RemoveRange(compromisos);
}
