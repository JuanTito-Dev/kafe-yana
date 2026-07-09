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

    /// <summary>
    /// Libera proporcionalmente solo la porción libre de stock comprometida para
    /// un detalle de ronda cuya cantidad se está reduciendo (edición o borrado
    /// parcial, cuando parte ya fue descontada por sub-ventas). Si
    /// <paramref name="cantidadALiberar"/> cubre toda la cantidad original, es
    /// equivalente a revertir el compromiso completo.
    /// </summary>
    Task RevertirCompromisoParcialAsync(int idDetalleRonda, int cantidadALiberar, int cantidadOriginalDetalle);

    Task AplicarMovimientosYCerrarAsync(int idPedido, string codigoVenta);
}
