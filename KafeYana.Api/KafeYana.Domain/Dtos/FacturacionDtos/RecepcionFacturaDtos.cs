using System;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    public class SolicitudRecepcionFacturaDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoDocumentoSector { get; set; }
        public int CodigoEmision { get; set; }
        public int CodigoModalidad { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cufd { get; set; } = string.Empty;
        public string Cuis { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int TipoFacturaDocumento { get; set; }
        public string Archivo { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public string HashArchivo { get; set; } = string.Empty;

        // NOTA: la asociación factura↔evento significativo NO viaja en el sobre
        // SOAP de `recepcionFactura` (WSDL del ServicioFacturacion). El campo
        // `codigoRecepcionEventoSignificativo` sólo existe en la operación
        // masiva `recepcionPaqueteFactura` (junto con `cantidadFacturas` y
        // `codigoEvento`). Refactor mayor — próxima iteración. Por ahora las
        // facturas contingencia se envían sin asociación explícita.
    }

    public class RespuestaRecepcionFacturaDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoRecepcion { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
        /// <summary>
        /// FIX #1 — para recepciones singulares la lista viene vacía; el campo
        /// existe para que el parser común (<c>ParsearRespuestaRecepcionFactura</c>)
        /// pueda propagar mensajesList cuando la operación es de paquete.
        /// </summary>
        public List<MensajeRecepcionPaqueteDto> MensajesList { get; set; } = new();
    }

    public class CodigoRespuestaSiatDto
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Solicitud para la operación SOAP <c>recepcionPaqueteFactura</c> del ServicioFacturacion.
    ///
    /// Wrapper WSDL: <c>SolicitudServicioRecepcionPaquete</c>.
    ///
    /// Hereda los 14 campos base de <see cref="SolicitudRecepcionFacturaDto"/> y agrega 3:
    /// <list type="bullet">
    ///   <item><c>CodigoEvento</c> (long): según WSDL es <c>xs:long</c>. Es el
    ///         <c>CodigoRecepcionEventoSignificativo</c> que devolvió el SIAT al
    ///         registrar el evento (NO el CodigoMotivo 1-7).</item>
    ///   <item><c>CantidadFacturas</c> (int): N facturas dentro del campo <c>archivo</c>
    ///         (que sigue siendo <c>xs:base64Binary</c>, igual que en <c>recepcionFactura</c>).</item>
    ///   <item><c>Cafc</c> (string?): opcional (<c>minOccurs="0"</c>).</item>
    /// </list>
    ///
    /// El campo <c>archivo</c> se mantiene como <c>xs:base64Binary</c> y contiene
    /// un archivo <b>TAR.GZ</b> con un entry <c>.xml</c> por factura (validado
    /// contra el piloto SIAT con 13 variantes en jun-2026 — solo TAR.GZ dio
    /// <c>transaccion=true</c>; ZIP/gzip/XML crudo fueron rechazados con 920).
    /// <c>cantidadFacturas</c> declarada debe coincidir con el N de entries del tar.
    /// Ver [[kafeyana-contingencia-paquete-siat]].
    /// </summary>
    public class SolicitudRecepcionPaqueteFacturaDto : SolicitudRecepcionFacturaDto
    {
        public long CodigoEvento { get; set; }
        public int CantidadFacturas { get; set; }
        public string? Cafc { get; set; } = string.Empty;

        // CodigoMotivo del evento significativo (1-7). Se usa en SiatHttpClient para
        // decidir si enviar <cafc>: sólo se incluye cuando el motivo es 5, 6 o 7
        // (factura manual / talonario físico). Para motivos 1-4 (sistema/caída),
        // el SIN espera que el campo <cafc> NO esté presente.
        public int? CodigoMotivo { get; set; }
    }

    /// <summary>
    /// Respuesta de <c>recepcionPaqueteFactura</c>. Misma estructura que
    /// <c>recepcionFacturaResponse</c>: <c>respuestaRecepcion</c> global con <c>transaccion</c>,
    /// <c>codigoRecepcion</c> único por paquete, <c>codigoEstado</c>, <c>codigoDescripcion</c>,
    /// y <c>mensajesList</c> (cada uno con <c>numeroArchivo</c>, <c>numeroDetalle</c>,
    /// <c>advertencia</c>).
    ///
    /// Mapeo: si Transaccion=true → codigoRecepcion global a todas las ventas del paquete
    /// (excepto las apuntadas por <c>numeroArchivo</c> en un mensaje de error).
    ///
    /// FIX #1 — mensajesList ahora viaja al caller (ReenvioFacturasContingenciaService.MapearRespuestaPaquete)
    /// para que rechazos individuales del SIN dentro del paquete (numeroArchivo=1..N)
    /// queden Observadas con la descripción del error, y no marcadas erróneamente como
    /// Validadas con el codRecep global. Antes el parser descartaba esta información
    /// silenciosamente.
    /// </summary>
    public class RespuestaRecepcionPaqueteFacturaDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoRecepcion { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
        public List<MensajeRecepcionPaqueteDto> MensajesList { get; set; } = new();
    }

    /// <summary>
    /// FIX #1 — entrada de <c>mensajesList</c> que devuelve el SIAT al recibir un paquete.
    /// <c>NumeroArchivo</c> (1..N) identifica qué entrada del TAR.GZ fue rechazada;
    /// <c>NumeroDetalle</c> identifica el ítem dentro de la factura si el rechazo es a nivel
    /// de detalle; <c>Advertencia</c> indica si el rechazo es fatal o solo informativo.
    /// </summary>
    public class MensajeRecepcionPaqueteDto
    {
        public string? NumeroArchivo { get; set; }
        public string? NumeroDetalle { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public string? Advertencia { get; set; }
    }

    /// <summary>
    /// FIX #1 — solicitud de la operación SOAP <c>validacionRecepcionPaqueteFactura</c>.
    /// Según la doc oficial del SIN (documentacion-contingencia.md líneas 26-28), tras
    /// enviar un paquete contingencia hay que consultar este servicio para conocer el
    /// estado real del procesamiento asíncrono del SIN: 901 (pendiente), 904 (observada)
    /// o 908 (validada). El backend actualmente NO lo consume — marcaba Validada apenas
    /// el SOAP síncrono devolvía transaccion=true, lo que producía falsos positivos.
    /// </summary>
    public class ValidacionRecepcionPaqueteDto
    {
        // ─── Identidad del paquete (orden del XSD del piloto, FIX #10 jun-2026) ───

        public string Cuis { get; set; } = string.Empty;
        public int CodigoAmbiente { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public int CodigoEmision { get; set; }
        public int TipoFacturaDocumento { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int CodigoSucursal { get; set; }
        public int CodigoDocumentoSector { get; set; }

        /// <summary>
        /// CUFD con el que se emitió el paquete que estamos validando. El piloto
        /// lo exige en la operación de validación (es parte del sobre
        /// <c>SolicitudServicioValidacionRecepcionPaquete</c>). Debe ser el CUFD
        /// del momento del envío, NO el CUFD vigente al momento de la consulta.
        /// Lo trae <c>EventoSignificativoSiat.CufdEvento</c>.
        /// </summary>
        public string Cufd { get; set; } = string.Empty;

        /// <summary>
        /// FIX #8 — codigoRecepcion del paquete. El piloto SIAT devuelve un GUID (ej:
        /// <c>a7689859-73c4-11f1-8b4f-9d6e0a3f236d</c>) en <c>recepcionPaqueteFactura</c>,
        /// NO un numérico como en <c>registroEventoSignificativo</c>. El WSDL declara
        /// <c>xs:long</c> pero el piloto lo trata como string al serializar. Tipar como
        /// string evita FormatException al validar y permite enviar el GUID tal cual al
        /// SOAP de <c>validacionRecepcionPaqueteFactura</c>.
        /// </summary>
        public string CodigoRecepcion { get; set; } = string.Empty;

        public int CodigoModalidad { get; set; }

        // ─── FIX #10 — campos obsoletos, ya NO se envían al sobre SOAP ───
        // El piloto SIAT rechaza codigoModulo y token en este servicio con
        // HTTP 500 ("Unmarshalling Error: unexpected element"). Se conservan
        // en el DTO para no romper el binding de los callers existentes,
        // pero el serializador en SiatHttpClient.ValidacionRecepcionPaqueteFacturaAsync
        // ya no los emite.

        [Obsolete("FIX #10: el piloto SIAT rechaza codigoModulo en validacionRecepcionPaqueteFactura. No enviar.")]
        public string CodigoModulo { get; set; } = string.Empty;

        [Obsolete("FIX #10: el piloto SIAT rechaza token en validacionRecepcionPaqueteFactura. No enviar.")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// FIX #1 — respuesta de <c>validacionRecepcionPaqueteFactura</c>. Igual shape que
    /// la respuesta de <c>recepcionPaqueteFactura</c>, pero el <c>CodigoEstado</c> es
    /// el definitivo (901/904/908), no el provisional del envío.
    /// </summary>
    public class RespuestaValidacionRecepcionPaqueteDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoRecepcion { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<MensajeRecepcionPaqueteDto> MensajesList { get; set; } = new();
    }
}
