using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Application.IRepositorio;

public interface IPedidoInventarioCompromisoRepositorio
{
    Task<PedidoInventarioComprometido?> ObtenerPorDetalleAsync(int idDetalleRonda);

    Task<List<PedidoInventarioComprometido>> ObtenerPorPedidoAsync(int idPedido);

    Task CrearAsync(PedidoInventarioComprometido compromiso);

    void Eliminar(PedidoInventarioComprometido compromiso);

    void EliminarRango(IEnumerable<PedidoInventarioComprometido> compromisos);
}
