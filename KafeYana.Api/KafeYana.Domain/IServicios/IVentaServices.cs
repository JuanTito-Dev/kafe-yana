using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios
{
    public interface IVentaServices
    {
        Task<ResultadoProcesarVenta> ProcesarVenta(DtoVentaPedido datos, string cajero);

        /// <summary>
        /// Genera una Venta (factura) a partir de una sub-venta ya cobrada, usando
        /// exclusivamente los datos ya copiados en <see cref="SubVenta.Detalles"/>
        /// (nunca datos vivos de la ronda de origen).
        /// </summary>
        Task<ResultadoProcesarVenta> ProcesarVentaDesdeSubVentaAsync(SubVenta subVenta, DtoVentaPedido datos, string cajero);

        /// <summary>
        /// Igual que <see cref="ProcesarVentaDesdeSubVentaAsync"/> pero sin emitir
        /// factura electrónica: construye una Venta con Facturado=false (mismo patrón
        /// que <see cref="ProcesarVenta"/> cuando <c>datos.Factura</c> es false), sin
        /// consumir correlativo SIAT.
        /// </summary>
        Task<ResultadoProcesarVenta> ProcesarVentaSinFacturaDesdeSubVentaAsync(SubVenta subVenta, DtoVentaPedido datos, string cajero);
    }
}


