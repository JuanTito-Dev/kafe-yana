using System;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Servicio de recepción SOAP para notas de crédito/débito.
    /// Espejo de IRecepcionFacturaService — diferencia clave: el sobre NO incluye Cufd.
    /// </summary>
    public interface IRecepcionNotaAjusteService
    {
        string CalcularHashArchivo(string archivo);

        Task<SolicitudRecepcionNotaAjusteDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            CancellationToken ct = default);

        Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            CancellationToken ct = default);
    }
}