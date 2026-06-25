using KafeYana.Domain.Entities.Facturacion;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface ICufdService
    {
        /// <summary>
        /// Solicita un CUFD al SIAT para una fechaEmision específica.
        /// La fechaEmision se persiste junto al CUFD para garantizar que el CUF
        /// generado después use EXACTAMENTE la misma fecha (el SIAT rechaza con 1002/1003
        /// si la fecha del CUF no coincide con la embebida en el CUFD).
        /// </summary>
        Task<Cufd> SolicitarCufdAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default);

        /// <summary>
        /// Devuelve el CUFD vigente en BD solo si su FechaEmisionSolicitud coincide
        /// (±2s de tolerancia por latencia) con la fechaEmision recibida. Si difieren
        /// más allá de la tolerancia o no hay CUFD vigente, solicita uno nuevo al SIAT.
        /// </summary>
        Task<Cufd> ObtenerCufdVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default);
    }
}