using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

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
        /// Notas de ajuste emitidas bajo un evento significativo (TipoEmision=2)
        /// que aún no fueron aceptadas por el SIAT. Orden FIFO por FechaEmision
        /// ASC para respetar el orden de correlativos al reenviar. Carga la nav
        /// prop <c>EventoSignificativoSiat</c> para no disparar queries N+1.
        ///
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        Task<List<NotaAjuste>> BuscarPendientesPorEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default);

        /// <summary>
        /// Reclamo retroactivo de las notas del "período gris": todas las que
        /// quedaron <c>EstadoSiat=Pendiente</c> sin <c>EventoSignificativoSiatId</c>
        /// en el rango de tiempo que cubre la contingencia. Las marca como
        /// <c>TipoEmision=2</c>, las vincula al evento, y limpia el
        /// <c>ErrorMensaje</c> (ya no son errores, son diferidas).
        ///
        /// Idempotente: el WHERE exige <c>EventoSignificativoSiatId IS NULL</c>,
        /// así que una segunda invocación no encuentra filas para actualizar.
        ///
        /// Retorna el número de filas afectadas. UPDATE directo vía
        /// <c>ExecuteUpdateAsync</c> (EF Core 7+), sin cargar entidades a memoria.
        /// </summary>
        Task<int> VincularNotasPendientesAlEventoAsync(
            int eventoSignificativoId,
            DateTime fechaInicioEvento,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

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

