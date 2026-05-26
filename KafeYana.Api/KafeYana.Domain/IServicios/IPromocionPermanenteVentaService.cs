using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios
{
    /// <summary>
    /// Evalúa y aplica promociones permanentes al cerrar una venta.
    /// Diseñado para extender PuntosExtra → Descuento → ProductoGratis.
    /// </summary>
    public interface IPromocionPermanenteVentaService
    {
        /// <summary>
        /// Procesa promos activas del tipo indicado. Devuelve la promo aplicada (máx. una por venta) o null.
        /// </summary>
        Task<ResultadoAplicacionPromocionPermanente?> ProcesarAlFinalizarVentaAsync(
            Cliente cliente,
            decimal totalVenta,
            string codigoVenta);
    }
}
