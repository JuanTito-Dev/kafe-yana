using System.Threading;
using System.Threading.Tasks;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Asigna correlativo SIAT, CUF/CUFD, leyenda, XML y hash a una nota de ajuste sin enviar.
    /// Valida que la venta referenciada esté en estado Validada y que la suma de subtotales
    /// coincida con montoTotalDevuelto (rechazo SIAT 1031/1029).
    /// </summary>
    public interface INotaAjusteSiatPreparer
    {
        Task PrepararNotaAsync(NotaAjuste nota, CancellationToken ct = default);
    }
}
