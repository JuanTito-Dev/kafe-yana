using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IVentaRepositorio : IGenericRepositorio<Venta>
    {
        IQueryable<Venta> VentaQuery();

        Task<int> ContarVentasDelAnio(int anio);

        Task<long> SiguienteNumeroFacturaSiatAsync();

        /// <summary>
        /// Correlativo SEPARADO para facturas emitidas bajo CAFC (contingencia
        /// motivos 5/6/7 — talonario/manual). El SIN autoriza un rango propio
        /// para el CAFC (ej. 1 al 1000), independiente del correlativo online
        /// normal (que ya va por decenas de miles). Usar el correlativo normal
        /// para estas ventas causa [1047] "NUMERO FACTURA PARA EL CAFC ENVIADO
        /// INCORRECTO" porque el número queda fuera del rango habilitado.
        /// Misma sequence de Postgres atómica que <see cref="SiguienteNumeroFacturaSiatAsync"/>,
        /// pero "Venta_NumeroFacturaCafc_seq" (creada manualmente, START 1).
        /// </summary>
        Task<long> SiguienteNumeroFacturaCafcAsync();

        Task<Venta?> TraerVentaConDetallesAsync(int id);

        /// <summary>
        /// Ventas emitidas bajo un evento significativo (TipoEmision=2) que aún
        /// no fueron aceptadas por el SIAT. Orden FIFO por FechaEmision ASC para
        /// respetar el orden de correlativos al reenviar. Carga la nav prop
        /// EventoSignificativoSiat para no disparar queries N+1.
        /// </summary>
        Task<List<Venta>> BuscarPendientesPorEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default);

        /// <summary>
        /// Reclamo retroactivo de las ventas del "período gris": todas las que
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
        Task<int> VincularVentasPendientesAlEventoAsync(
            int eventoSignificativoId,
            DateTime fechaInicioEvento,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

        /// <summary>
        /// Marca todas las ventas Pendientes (EstadoSiat=Pendiente) asociadas a un
        /// evento contingencia con un ErrorMensaje. Usado por
        /// <c>EventoSignificativoSiatService.AutoExpirarEventoAsync</c> cuando el
        /// monitor auto-expira un evento viejo en el boot (Gap 6).
        /// Devuelve la cantidad de ventas actualizadas.
        ///
        /// NO marca ventas ya Validadas/Anuladas/Observadas — sólo Pendientes
        /// (las que aún no llegaron al SIAT). UPDATE bulk vía
        /// <c>ExecuteUpdateAsync</c>.
        /// </summary>
        Task<int> MarcarVentasContingenciaExpiradaAsync(
            int eventoSignificativoId,
            string mensajeError,
            CancellationToken ct = default);

        /// <summary>
        /// Marca con ErrorMensaje las ventas contingencia (TipoEmision=2) que
        /// quedaron con CUF malformado por el bug pre-Gap-7 (donde se concatenaba
        /// el CUFD base64 en lugar del CodigoControl hex del CUFD). El CUF NO se
        /// regenera retroactivamente porque reconstruirlo requiere
        /// <c>cufd.FechaEmisionSolicitud</c> exacta y eso causaría 1002/1003 al
        /// reenviar. Verificación: el operador las anulará manualmente desde la UI.
        ///
        /// Criterio de "CUF malformado":
        ///   - TipoEmision = 2 (contingencia)
        ///   - EstadoSiat = Pendiente
        ///   - ErrorMensaje IS NULL  (idempotente: WHERE evita re-marcar)
        ///   - LENGTH(Cuf) &lt; 50 OR LENGTH(Cuf) > 80 OR Cuf contiene "=="
        ///
        /// Retorna el número de filas marcadas. UPDATE bulk vía
        /// <c>ExecuteUpdateAsync</c> (EF Core 7+, sin tracking).
        /// </summary>
        Task<int> MarcarVentasCufMalformadoAsync(
            string mensajeError,
            CancellationToken ct = default);

        /// <summary>
        /// FIX #4 — sweep de ventas contingencia huérfanas: marcadas como contingencia
        /// (TipoEmision=2) pero sin evento significativo asociado (FK null). Esto pasa
        /// cuando el cobro entra al catch de SiatOfflineException del
        /// FacturaSiatEnvioService antes de que el monitor haya cruzado el umbral y
        /// persistido el evento (período gris). VincularVentasPendientesAlEventoAsync
        /// NO las rescata porque filtra FechaEmision &gt;= FechaHoraInicioEvento
        /// (existe una ventana donde la venta queda fuera del rango).
        ///
        /// Devuelve las filas completas (no un conteo) porque el caller las reencola
        /// online con EnviarVentaAsync tras reclasificarlas a TipoEmision=1.
        /// Ver [[kafeyana-contingencia-984-rescate]] y
        /// [[kafeyana-contingencia-siat]].
        /// </summary>
        Task<List<Venta>> BuscarPendientesSinEventoAsync(CancellationToken ct = default);

        /// <summary>
        /// FIX #6 — ventas contingencia pendientes VINCULADAS a un evento específico
        /// (TipoEmision=2 + EventoSignificativoSiatId = eventoId + EstadoSiat=Pendiente).
        /// Espejo de <see cref="BuscarPendientesSinEventoAsync"/> pero con FK poblada.
        /// Se usa para el rescate desatendido tras la cascada 984: el evento persiste
        /// como <c>Rechazado</c> y las ventas vinculadas necesitan salir del
        /// contingency path (que exige CodigoRecepcionEventoSignificativo) hacia el
        /// online path vía <see cref="BuscarPendientesSinEventoAsync"/>.
        /// El orden FIFO por FechaEmision ASC preserva la trazabilidad temporal.
        /// Incluye <c>Detalles</c> para que el preparer no genere N+1 al regenerar.
        /// Ver [[kafeyana-contingencia-984-rescate]].
        /// </summary>
        Task<List<Venta>> BuscarPendientesPorEventoIdAsync(
            int eventoId,
            CancellationToken ct = default);
    }
}
