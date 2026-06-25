using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IFacturaImpresionService
    {
        /// <summary>
        /// Imprime la representación gráfica (ESC/POS) de la factura de la
        /// venta indicada, enviándola a uno o varios destinos de la sección
        /// `Impresoras.Destinos` del appsettings (principal/cocina/barra).
        ///
        /// - Si `destinos` está vacío, devuelve <c>Ok=false</c>.
        /// - Si un destino no existe en la config, ese envío falla pero los
        ///   demás siguen.
        /// - El ticket se construye UNA sola vez con `anchoCaracteres`
        ///   (o el default de la config si es null).
        /// </summary>
        Task<ResultadoImpresionFacturaDto> ImprimirPorIdAsync(
            int ventaId,
            IReadOnlyList<string> destinos,
            int? anchoCaracteres,
            CancellationToken ct = default);
    }
}