using KafeYana.Application.Dtos.FacturacionDtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IRecepcionFacturaService
    {
        string CalcularHashArchivo(string archivo);

        Task<SolicitudRecepcionFacturaDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            CancellationToken ct = default);

        Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            CancellationToken ct = default);
    }
}