using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Servicio que obtiene la fecha/hora oficial del SIN antes de generar
    /// una factura o nota de ajuste.
    /// </summary>
    public interface IFechaHoraSiatService
    {
        /// <summary>
        /// Llama al SOAP sincronizarFechaHora del SIAT y devuelve la fecha/hora
        /// oficial. Si el SIAT no responde, lanza VentaException para bloquear
        /// la facturación (no se permite usar hora local).
        /// </summary>
        Task<System.DateTime> ObtenerFechaHoraOficialAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);
    }
}