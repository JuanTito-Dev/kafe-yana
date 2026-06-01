using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Application.IServicios;

public interface IRondaPedidoService
{
    Task<Ronda> EditarRondaAsync(int idRonda, DtoRondaEditar datos);

    Task EliminarRondaAsync(int idRonda, int idPedido);

    Task<Detalle_ronda> EditarDetalleAsync(int idDetalle, DtoRondadetalle datos, int idPedido);

    Task EliminarDetalleAsync(int idDetalle, int idPedido);

    Task RecalcularTotalPedidoAsync(int idPedido);
}
