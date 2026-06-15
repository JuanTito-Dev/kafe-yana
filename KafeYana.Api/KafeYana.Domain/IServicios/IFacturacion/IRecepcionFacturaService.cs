using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IRecepcionFacturaService
    {
        string CalcularHashArchivo(string archivo);

        Task<SolicitudRecepcionFacturaDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            CancellationToken ct = default);

        Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            CancellationToken ct = default);
    }
}
