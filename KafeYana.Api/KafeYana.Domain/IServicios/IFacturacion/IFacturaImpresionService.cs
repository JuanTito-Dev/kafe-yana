using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IFacturaImpresionService
    {
        Task<ResultadoImpresionFacturaDto> ImprimirVentaAsync(
            Venta venta,
            CancellationToken ct = default);

        Task<ResultadoImpresionFacturaDto> ImprimirPorIdAsync(
            int ventaId,
            CancellationToken ct = default);
    }
}
