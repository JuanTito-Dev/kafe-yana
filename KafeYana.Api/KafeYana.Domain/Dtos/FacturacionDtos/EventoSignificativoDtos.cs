using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Body para registrar un evento significativo ante el SIN. La fecha de fin
    /// puede omitirse: si el evento sigue activo al momento de llamar, se asume
    /// fechaInicio + 2 minutos como placeholder (el SIAT acepta eventos cortos).
    ///
    /// <b>Flujo unificado para los 7 motivos del SIN (1-7):</b>
    /// <list type="bullet">
    ///   <item>1 — CORTE DEL SERVICIO DE INTERNET</item>
    ///   <item>2 — INACCESIBILIDAD AL SERVICIO WEB DE LA ADMINISTRACIÓN TRIBUTARIA</item>
    ///   <item>3 — INGRESO A ZONAS SIN INTERNET POR DESPLIEGUE DE PUNTO DE VENTA</item>
    ///   <item>4 — VENTA EN LUGARES SIN INTERNET</item>
    ///   <item>5 — VIRUS INFORMÁTICO O FALLA DE SOFTWARE</item>
    ///   <item>6 — CAMBIO DE INFRAESTRUCTURA DE SISTEMA O FALLA DE HARDWARE</item>
    ///   <item>7 — CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA</item>
    /// </list>
    /// Si <see cref="Descripcion"/> viene vacía, el backend la rellena
    /// automáticamente desde el catálogo sincronizado del SIAT.
    /// </summary>
    public class DtoRegistrarEventoSignificativo
    {
        /// <summary>Código del motivo (1..7 según CatEventosSignificativos).</summary>
        public int CodigoMotivo { get; set; }

        /// <summary>
        /// Descripción libre de la causa (opcional). Si viene vacía, el backend
        /// usa la descripción oficial del catálogo para el motivo elegido
        /// (ej: motivo=4 → "VENTA EN LUGARES SIN INTERNET"). Máximo 500 chars.
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>Inicio del corte. Si se omite, el backend usa la hora UTC actual.</summary>
        public DateTime? FechaHoraInicioEvento { get; set; }

        /// <summary>
        /// Fin del corte. Si se omite, el backend asume FechaInicio + 2 min (evento corto).
        /// Si la contingencia aún está activa cuando se recupera el SIAT, este valor se actualiza.
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }

        /// <summary>Sucursal del PV que sufrió la contingencia (default: appsettings).</summary>
        public int? CodigoSucursal { get; set; }

        /// <summary>Punto de venta del PV que sufrió la contingencia (default: appsettings).</summary>
        public int? CodigoPuntoVenta { get; set; }

        /// <summary>Usuario que dispara el registro (cajero). Opcional.</summary>
        public string? Usuario { get; set; }

        /// <summary>"Manual" (cajero desde UI) o "Automatico" (wrapper detector). Default Manual.</summary>
        public string? Origen { get; set; }
    }

    /// <summary>
    /// Respuesta al registrar un evento significativo. <see cref="CodigoRecepcionEventoSignificativo"/>
    /// es el comprobante legal: debe guardarse y presentarse ante el SIN si auditoría.
    /// </summary>
    public class ResultadoRegistroEventoSignificativoDto
    {
        public bool Transaccion { get; set; }

        public string? CodigoRecepcionEventoSignificativo { get; set; }

        public int EventoId { get; set; }

        public DateTime FechaHoraInicioEvento { get; set; }

        /// <summary>
        /// Fecha/hora de fin. NULL hasta que <c>ReenviarRegistroAsync</c> la
        /// popula con la hora real de recuperación antes del SOAP. El SIAT
        /// rechaza rangos cortos placeholder con error 981.
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }

        public string EstadoContingencia { get; set; } = "Activo";

        public string? CodigoDescripcion { get; set; }

        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    /// <summary>
    /// DTO de entrada del sobre SOAP <c>registroEventoSignificativo</c> hacia el SIAT.
    /// Espejo del request que arma <c>SiatHttpClient</c>.
    /// </summary>
    public class SolicitudRegistroEventoSignificativoDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string CufdEvento { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHoraInicioEvento { get; set; }
        public DateTime FechaHoraFinEvento { get; set; }
        public long Nit { get; set; }
        public int CodigoMotivoEvento { get; set; }
    }

    /// <summary>
    /// Respuesta cruda del SIAT para <c>registroEventoSignificativoResponse</c>.
    /// El wrapper del SIAT es <c>RespuestaListaEventos</c> (en plural) y trae
    /// <c>codigoRecepcionEventoSignificativo</c> + <c>transaccion</c>.
    /// </summary>
    public class RespuestaRegistroEventoSignificativoDto
    {
        public bool Transaccion { get; set; }
        public string? CodigoRecepcionEventoSignificativo { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    /// <summary>
    /// Estado consolidado de la contingencia para exponer en GET /estado.
    /// Cuando <see cref="ContingenciaActiva"/> es true, el ERP está en modo offline
    /// y debe emitir facturas citando <see cref="CodigoRecepcionEventoSignificativo"/>.
    /// </summary>
    public class EstadoContingenciaDto
    {
        public bool ContingenciaActiva { get; set; }

        public int? EventoSignificativoId { get; set; }

        public string? CodigoRecepcionEventoSignificativo { get; set; }

        /// <summary>
        /// CUFD vigente al momento de registrar el evento. Se reusa como CUFD
        /// de las facturas emitidas en contingencia (la asociación con el SIN
        /// se hace vía <see cref="CodigoRecepcionEventoSignificativo"/> en el
        /// sobre SOAP, no por consistencia de CUFD como en línea).
        /// </summary>
        public string? CufdEvento { get; set; }

        /// <summary>
        /// Hex de 15-16 chars del CUFD del momento del evento. Concatenado al
        /// CUF de cada factura contingencia (mismo rol que <c>cufd.CodigoControl</c>
        /// en línea, ver [[kafeyana-cuf-cufd-fechaemision]]). NULL en eventos
        /// pre-Gap-7.
        /// </summary>
        public string? CodigoControlEvento { get; set; }

        public int? CodigoMotivo { get; set; }

        public string? DescripcionMotivo { get; set; }

        public DateTime? FechaHoraInicioEvento { get; set; }

        public int? CodigoSucursal { get; set; }

        public int? CodigoPuntoVenta { get; set; }

        public string Origen { get; set; } = string.Empty;

        /// <summary>
        /// Fecha/hora de fin del corte. NULL mientras el evento está pendiente
        /// de envío al SIAT (modo degradado / AutomaticoSinSoap).
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }
    }

    /// <summary>DTO liviano para listar historial de contingencias.</summary>
    public class EventoSignificativoHistorialDto
    {
        public int Id { get; set; }

        public int CodigoMotivo { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("descripcionMotivoCatalogo")]
        public string? DescripcionMotivoCatalogo { get; set; }

        public DateTime FechaHoraInicioEvento { get; set; }

        /// <summary>
        /// Fecha/hora de fin del corte. NULL mientras el evento está pendiente
        /// de envío al SIAT (modo degradado / AutomaticoSinSoap).
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }

        public string? CodigoRecepcionEventoSignificativo { get; set; }

        public bool Transaccion { get; set; }

        public string Origen { get; set; } = string.Empty;

        public string? Usuario { get; set; }

        public string EstadoContingencia { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaCierre { get; set; }
    }

    /// <summary>
    /// Snapshot reducido de una contingencia activa. Lo consume el monitor de
    /// conectividad al boot para hidratar su diccionario interno sin pasar por
    /// <see cref="EstadoContingenciaDto"/> (que carga descripciones de catálogo).
    /// </summary>
    public class ContingenciaActivaDto
    {
        public int EventoSignificativoId { get; set; }
        public int CodigoSucursal { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string? CodigoRecepcionEventoSignificativo { get; set; }
        public int CodigoMotivo { get; set; }
        public DateTime FechaHoraInicioEvento { get; set; }

        /// <summary>
        /// Fecha/hora de fin del corte. NULL mientras el evento está pendiente
        /// de envío al SIAT (modo degradado / AutomaticoSinSoap).
        /// </summary>
        public DateTime? FechaHoraFinEvento { get; set; }

        public string Origen { get; set; } = string.Empty;
    }
}