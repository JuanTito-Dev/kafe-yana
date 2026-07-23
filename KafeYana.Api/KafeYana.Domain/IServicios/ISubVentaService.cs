using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios
{
    public interface ISubVentaService
    {
        /// <summary>
        /// Cobra una porción de lo consumido en un pedido (mesa o para llevar):
        /// descuenta FIFO across rondas del/los producto(s) pedidos, bajo un
        /// advisory lock por pedido (real, serializa cobros concurrentes del
        /// mismo pedido). Si <paramref name="datos"/>.Factura es true, factura
        /// de inmediato contra los datos ya copiados en la sub-venta.
        /// </summary>
        Task<ResultadoCobroPedidoDto> CrearSubVentaAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            string origenVenta,
            Action liberarPedido,
            int? idMesa,
            CancellationToken ct = default);

        /// <summary>Revierte una sub-venta no facturada: repone CantidadDescontada y la elimina.</summary>
        Task<DtoPedidoActualizado> RevertirSubVentaAsync(int subVentaId, CancellationToken ct = default);

        /// <summary>Factura después una sub-venta que se cobró sin factura al momento.</summary>
        Task<ResultadoEnvioFacturaSiatDto?> FacturarSubVentaAsync(
            int subVentaId, DtoFacturarSubVenta datos, string cajero, CancellationToken ct = default);

        Task<List<DtoSubVentaPendiente>> GetPendientesFacturarAsync(int? idMesa = null, int? idParaLlevar = null, CancellationToken ct = default);

        /// <summary>
        /// Historial completo de sub-ventas de un pedido (facturadas y no) —
        /// fuente de verdad en BD para que la UI no dependa de estado local
        /// de sesión (sobrevive a refresh, otra pestaña, otro dispositivo).
        /// </summary>
        Task<List<DtoSubVentaPendiente>> GetPorPedidoAsync(int idPedido, CancellationToken ct = default);
    }
}
