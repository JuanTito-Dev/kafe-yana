using KafeYana.Domain.Dtos.InventarioPedido;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Application.IServicios;

public interface IInventarioPedidoCompromisoService
{
    PedidoInventarioComprometido CrearCompromiso(
        int idPedido,
        string referencia,
        List<CompromisoLineaCalculo> lineas);

    Task RevertirCompromisoAsync(PedidoInventarioComprometido compromiso);

    Task RevertirCompromisoPorDetalleAsync(int idDetalleRonda);

    Task AplicarMovimientosYCerrarAsync(int idPedido, string codigoVenta);
}
