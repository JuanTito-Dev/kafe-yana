using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;

namespace KafeYana.Domain.Entities.Facturacion
{
    /// <summary>
    /// Log de cada evento significativo registrado ante el SIN vía
    /// <c>registroEventoSignificativo</c>. Guarda tanto el REQUEST enviado como
    /// la RESPUESTA del SIAT (incluyendo <c>codigoRecepcionEventoSignificativo</c>,
    /// que es el comprobante legal de la notificación).
    ///
    /// Reglas del SIN Bolivia:
    ///   • Una contingencia se considera ACTIVA cuando existe al menos una fila
    ///     con <see cref="EstadoContingencia"/> = Activo (no se cerró la factura
    ///     del corte).
    ///   • Mientras esté activa, el ERP puede emitir facturas "fuera de línea"
    ///     (tipoEmision=2) o en contingencia (tipoEmision=4) citando el
    ///     <see cref="CodigoRecepcionEventoSignificativo"/> en los metadatos de
    ///     cada factura, para que el SIN las vincule al evento registrado.
    ///   • Al recuperar la conexión, se cierra el evento (EstadoContingencia =
    ///     Cerrado) y se re-envían las facturas pendientes a través de
    ///     <c>recepcionFactura</c> con la metadata del evento.
    ///
    /// Esta tabla es la única fuente de verdad del estado de contingencia:
    /// Ver [[kafeyana-contingencia-siat]] — backend autoridad, frontend reacciona.
    /// </summary>
    public class EventoSignificativoSiat : BaseEntity
    {
        // ─── REQUEST enviado al SIAT ─────────────────────────────────────

        /// <summary>Código del motivo (1..7) según catálogo CatEventoSignificativo.</summary>
        public int CodigoMotivo { get; set; }

        /// <summary>Descripción libre que el operador (o el sistema) envió como causa del evento.</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Fecha/hora UTC de inicio del corte.</summary>
        public DateTime FechaHoraInicioEvento { get; set; }

        /// <summary>
        /// Fecha/hora UTC de fin del corte. NULL mientras el evento está
        /// pendiente de envío al SIAT (modo degradado / AutomaticoSinSoap) —
        /// NO se persiste con un valor placeholder porque el SIAT rechaza con
        /// 981 "RANGO DE FECHAS DE EVENTO SIGNIFICATIVO INVALIDO" rangos
        /// artificialmente cortos. Se setea en <c>ReenviarRegistroAsync</c>
        /// con la hora real de la recuperación, justo antes del SOAP.
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }

        public int CodigoAmbiente { get; set; }

        public int CodigoPuntoVenta { get; set; }

        public int CodigoSucursal { get; set; }

        public string CodigoSistema { get; set; } = string.Empty;

        public long Nit { get; set; }

        /// <summary>CUFD vigente al momento del registro (parámetro obligatorio del sobre SOAP).</summary>
        public string Cufd { get; set; } = string.Empty;

        /// <summary>CUFD vigente al momento del evento (parámetro obligatorio del sobre SOAP).</summary>
        public string CufdEvento { get; set; } = string.Empty;

        /// <summary>
        /// Hex de 15-16 chars que el SIAT embebió en el CUFD del momento del corte.
        /// Se concatena al CUF de cada factura emitida en contingencia (mismo rol
        /// que <c>cufd.CodigoControl</c> en línea, ver [[kafeyana-cuf-cufd-fechaemision]]).
        /// Distinto de <see cref="Cufd"/> (que es el CUFD base64 que viaja como
        /// parámetro SOAP del sobre de la factura contingencia).
        /// NULL en eventos pre-Gap-7. Se popula en
        /// <c>RegistrarYActivarAsync</c> / <c>RegistrarLocalmenteSinSoapAsync</c> /
        /// <c>ReenviarRegistroAsync</c>.
        /// </summary>
        public string? CodigoControlEvento { get; set; }

        public string Cuis { get; set; } = string.Empty;

        // ─── RESPONSE del SIAT ───────────────────────────────────────────

        /// <summary>
        /// Comprobante legal devuelto por el SIN de que la contingencia fue
        /// notificada. NO es decorativo: es lo que se presenta ante una
        /// auditoría y se vincula en los metadatos de cada factura emitida
        /// durante el corte.
        /// </summary>
        public string? CodigoRecepcionEventoSignificativo { get; set; }

        /// <summary>true si el SIAT aceptó el registro (transaccion=true).</summary>
        public bool Transaccion { get; set; }

        /// <summary>Códigos de error devueltos por el SIAT (uno por línea en CodigosRespuesta).</summary>
        public string CodigosRespuestaJson { get; set; } = "[]";

        // ─── Metadata local ──────────────────────────────────────────────

        /// <summary>Origen del registro: Manual (cajero desde UI) o Automatico (wrapper detector).</summary>
        public string Origen { get; set; } = "Manual";

        public string? Usuario { get; set; }

        /// <summary>
        /// <c>Activo</c> = contingencia en curso (facturas se siguen
        /// emitiendo offline con metadata de este evento). <c>Cerrado</c>
        /// = SIAT respondió OK al evento, ya no se debe seguir emitiendo con
        /// este codigoRecepcion. <c>Rechazado</c> = el SIAT rechazó el
        /// registro (ej: fechas inválidas, datos inconsistentes) y NO se
        /// reintenta automáticamente; requiere revisión del operador.
        /// </summary>
        public string EstadoContingencia { get; set; } = EventoContingenciaEstado.Activo;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        /// <summary>Última actualización (ej: cuando se cierra la contingencia al recuperar SIAT).</summary>
        public DateTime? FechaCierre { get; set; }
    }

    /// <summary>Estados posibles del evento de contingencia en BD local.</summary>
    public static class EventoContingenciaEstado
    {
        public const string Activo = "Activo";
        public const string Cerrado = "Cerrado";

        /// <summary>
        /// El SIAT rechazó el registro del evento (ej: 981 fechas inválidas,
        /// datos inconsistentes). Se persiste así en lugar de <c>Cerrado</c>
        /// para que el monitor NO lo rehidrate en el próximo boot y para que
        /// el operador lo pueda distinguir de un cierre exitoso. NO se
        /// reintenta automáticamente.
        /// </summary>
        public const string Rechazado = "Rechazado";

        /// <summary>
        /// Estado terminal asignado por el monitor al boot cuando el evento
        /// activo excedió <c>SiatOptions.HorasMaximaContingenciaAbierta</c>
        /// (default 48h) sin cerrarse. El SIN ya no aceptará su registro
        /// por error 981 (rango de fechas inválido). Las ventas asociadas
        /// quedan en <c>EstadoSiat=Pendiente</c> con <c>ErrorMensaje</c>
        /// claro para acción manual del operador (anular o desvincular).
        /// </summary>
        public const string AutoExpirado = "AutoExpirado";

        public static bool EsValido(string estado) =>
            estado == Activo || estado == Cerrado
            || estado == Rechazado || estado == AutoExpirado;
    }
}