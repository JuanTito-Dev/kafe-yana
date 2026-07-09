using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios
{
    public interface ICobroPedidoService
    {
        Task<ResultadoCobroPedidoDto> CobrarPedidoActivoAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default);

        Task<ResultadoCobroPedidoDto> CobrarMesaAsync(
            int idMesa,
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default);

        Task<ResultadoCobroPedidoDto> CobrarParaLlevarAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default);

        Task<DtoPedidoActualizado> RevertirAbonoAsync(int abonoId, CancellationToken ct = default);
    }
}
