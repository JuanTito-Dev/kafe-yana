using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Orquestador: crea la entidad NotaAjuste a partir de un DTO + Venta existente,
    /// llama al preparer, envía al SIAT y persiste el resultado.
    /// </summary>
    public interface INotaAjusteSiatEnvioService
    {
        Task<ResultadoEnvioNotaAjusteSiatDto> EmitirYEnviarNotaAsync(
            int ventaId,
            DtoCrearNotaAjuste dto,
            CancellationToken ct = default);

        Task<ResultadoEnvioNotaAjusteSiatDto> ReenviarNotaAsync(
            int notaAjusteId,
            CancellationToken ct = default);
    }
}
