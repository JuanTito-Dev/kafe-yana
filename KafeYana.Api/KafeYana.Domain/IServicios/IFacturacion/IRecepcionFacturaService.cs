using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Facturacion;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Overload para reenvío de facturas emitidas en contingencia (TipoEmision=2).
        /// Gap 12: usa internamente <c>recepcionPaqueteFactura</c> con 1 sola venta,
        /// porque el método SOAP singular <c>recepcionFactura</c> SOLO acepta
        /// CodigoEmision=1 (online) — SIAT rechaza con 916 cuando se le envía 2.
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        Task<RespuestaRecepcionFacturaDto> EnviarRecepcionContingenciaAsync(
            Venta venta,
            EventoSignificativoSiat evento,
            CancellationToken ct = default);

        /// <summary>
        /// Envía un PAQUETE de N facturas contingencia a la operación SOAP
        /// <c>recepcionPaqueteFactura</c> del ServicioFacturacionCompraVenta.
        ///
        /// Esta es la vía oficial para que la asociación factura↔evento significativo
        /// quede registrada en el sobre SOAP (campo <c>codigoEvento</c> =
        /// <c>CodigoRecepcionEventoSignificativo</c> del evento).
        ///
        /// Pre-condiciones:
        /// - 1 &lt;= ventas.Count &lt;= CantidadMaximaPaquete.
        /// - Todas las ventas comparten (suc, pv, evento, cufd).
        /// - Cada venta tiene XmlBase64 (gzip+base64) poblado.
        /// - evento.CodigoRecepcionEventoSignificativo poblado.
        ///
        /// Ver [[kafeyana-contingencia-paquete-siat]].
        /// </summary>
        Task<RespuestaRecepcionPaqueteFacturaDto> EnviarRecepcionPaqueteContingenciaAsync(
            IReadOnlyList<Venta> ventas,
            EventoSignificativoSiat evento,
            CancellationToken ct = default);

        /// <summary>
        /// FIX #1 — consulta el estado real del paquete enviado al SIAT (901 pendiente,
        /// 904 observada, 908 validada). Implementa la operación SOAP
        /// <c>validacionRecepcionPaqueteFactura</c> documentada en el SIN pero
        /// históricamente no consumida por el backend (que marcaba Validada apenas
        /// el SOAP síncrono respondía transaccion=true).
        /// Ver documentacion-contingencia.md líneas 26-28.
        ///
        /// FIX #8 — <paramref name="codigoRecepcion"/> es <c>string</c> (no <c>long</c>):
        /// el piloto SIAT devuelve un GUID (ej: <c>a7689859-73c4-11f1-8b4f-9d6e0a3f236d</c>)
        /// en <c>recepcionPaqueteFactura</c>, no un numérico como en
        /// <c>registroEventoSignificativo</c>. El WSDL declara <c>xs:long</c> pero el
        /// piloto lo serializa como string; respetar el formato que el piloto devuelve.
        ///
        /// FIX #10 — <paramref name="cufd"/> es el CUFD del momento del ENVÍO del paquete
        /// (no el vigente al validar). El piloto SIAT lo exige en
        /// <c>validacionRecepcionPaqueteFactura</c> y rechaza con HTTP 500 si falta.
        /// Lo trae <c>EventoSignificativoSiat.CufdEvento</c>.
        /// </summary>
        Task<RespuestaValidacionRecepcionPaqueteDto> ValidarRecepcionPaqueteContingenciaAsync(
            string codigoRecepcion,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoAmbiente,
            string cuis,
            string cufd,
            string codigoSistema,
            long nit,
            CancellationToken ct = default);
    }
}