using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IFacturaSiatEnvioService
    {
        Task<ResultadoEnvioFacturaSiatDto> EnviarVentaAsync(
            Venta venta,
            CancellationToken ct = default);

        Task<ResultadoEnvioFacturaSiatDto> ReenviarFacturaAsync(
            int ventaId,
            DtoDatosFiscalesReenvio? datosFiscales = null,
            CancellationToken ct = default);
    }
}
