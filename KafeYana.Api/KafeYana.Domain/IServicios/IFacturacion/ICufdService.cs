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
        ///
        /// <para>
        /// <c>bypassCortocircuito=true</c> omite el cortocircuito del monitor de
        /// contingencia. Usar sólo cuando el llamador ya confirmó que SIAT está
        /// alcanzable (ej: Pieza 3b <c>ReenviarRegistroAsync</c>, ejecutado
        /// justo después de que el probe detectó recuperación).
        /// </para>
        /// </summary>
        Task<Cufd> SolicitarCufdAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default,
            bool bypassCortocircuito = false);

        /// <summary>
        /// Devuelve el CUFD vigente en BD solo si su FechaEmisionSolicitud coincide
        /// (±2s de tolerancia por latencia) con la fechaEmision recibida. Si difieren
        /// más allá de la tolerancia o no hay CUFD vigente, solicita uno nuevo al SIAT.
        ///
        /// <para>
        /// <c>bypassCortocircuito=true</c> se propaga a <see cref="SolicitarCufdAsync"/>.
        /// </para>
        ///
        /// <para>
        /// <c>esContingencia=true</c> relaja el umbral de antigüedad a 24 horas (en
        /// línea usa 5 min para evitar el error SIAT 1009). Para contingencia el SIAT
        /// no compara contra hora actual, así que se puede reusar durante toda la
        /// vigencia oficial del CUFD. Usar en flujos de registro de eventos
        /// significativos y reenvíos de paquetes contingencia.
        /// </para>
        /// </summary>
        Task<Cufd> ObtenerCufdVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default,
            bool bypassCortocircuito = false,
            bool esContingencia = false);

        /// <summary>
        /// Devuelve el CUFD más reciente de BD con <c>FechaVigencia &gt; NOW</c> sin
        /// importar su antigüedad (la lógica de <c>AntiguedadMaximaCufd</c> NO se aplica).
        /// Si no hay ninguno vigente, devuelve el CUFD más reciente registrado en BD
        /// (incluyendo los ya vencidos), o <c>null</c> si la tabla está vacía.
        ///
        /// NO toca el SIAT. Diseñado para que el monitor de contingencia y el registro
        /// local de eventos significativos puedan obtener un CUFD mientras el SIN está
        /// caído. El Cufd devuelto no debe usarse para facturar online: el reenvío
        /// post-recuperación debe obtener un CUFD fresco con <see cref="ObtenerCufdVigenteAsync"/>
        /// antes de generar el CUF.
        ///
        /// Ver [[kafeyana-contingencia-siat]] — registro local de eventos sin SOAP.
        /// </summary>
        Task<Cufd?> ObtenerCufdEnCacheAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);
    }
}