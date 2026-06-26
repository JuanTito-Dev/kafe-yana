using KafeYana.Application.Dtos.FacturacionDtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IRecepcionFacturaService
    {
        string CalcularHashArchivo(string archivo);

        /// <summary>
        /// Prepara el sobre SOAP de "recepcionFactura".
        /// Si el caller ya resolvió el CUFD (lo pasó al generar el CUF), puede
        /// prefijarlo en <paramref name="cufdPrefijo"/> para que el sobre use
        /// EXACTAMENTE ese mismo CUFD y no uno nuevo. Esto evita el bug de
        /// divergencia CUF/CUFD que el SIAT rechaza con 1002/1003.
        /// Si <paramref name="cufdPrefijo"/> es null/empty, se obtiene un CUFD
        /// vigente de forma independiente (comportamiento legacy).
        /// </summary>
        Task<SolicitudRecepcionFacturaDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default);

        /// <summary>
        /// Prepara el sobre y lo envía al SIAT. Ver <see cref="PrepararSolicitudAsync"/>
        /// para el contrato de los parámetros opcionales.
        /// </summary>
        Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default);
    }
}