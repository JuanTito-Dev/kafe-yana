using System.Linq;
using System.Threading.Tasks;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface INotaAjusteRepositorio : IGenericRepositorio<NotaAjuste>
    {
        IQueryable<NotaAjuste> NotaAjusteQuery();

        /// <summary>Correlativo SIAT: MAX(NumeroNotaCreditoDebito) + 1 entre todas las notas.</summary>
        Task<long> SiguienteNumeroNotaCreditoDebitoAsync();

        Task<NotaAjuste?> TraerNotaAjusteConDetallesAsync(int id);

        Task<IReadOnlyList<NotaAjuste>> ListarPorVentaAsync(int ventaId);

        /// <summary>
        /// Devuelve un mapa <c>IdDetallePago → cantidad ya devuelta</c> para todas
        /// las notas de la venta indicada. La suma se calcula sobre las líneas
        /// <c>CodigoDetalleTransaccion = 2</c> (la devolución efectiva), filtrando
        /// por notas en estado SIAT = <c>Validada</c> (alineado con la regla del
        /// frontend en <c>sales.mapper.ts:85-86</c>).
        ///
        /// Se usa para validar que una nueva nota no exceda la cantidad ya
        /// devuelta por producto y para alimentar el resolver GraphQL
        /// <c>DetallePago.cantidadDevuelta</c>.
        /// </summary>
        Task<System.Collections.Generic.Dictionary<int, decimal>> ObtenerCantidadDevueltaPorDetallePagoAsync(int ventaId);
    }
}
