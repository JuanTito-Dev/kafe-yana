namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Configuración de un punto de venta del sistema ante el SIAT.
    /// Permite manejar N sucursales/puntos de venta con una sola instalación del software.
    ///
    /// El CUIS/CUFD NO se guarda aquí porque ya existen las tablas Cuis y Cufd
    /// con vigencia propia. Se consultan dinámicamente por (sucursal, puntoVenta).
    /// </summary>
    public class PuntoVentaSiat
    {
        public int Id { get; set; }

        /// <summary>Código de sucursal asignado por el SIN (0 = Casa Matriz).</summary>
        public int CodigoSucursal { get; set; }

        /// <summary>Código de punto de venta asignado por el SIN (0 = único, 1+ = múltiples).</summary>
        public int CodigoPuntoVenta { get; set; }

        /// <summary>Nombre descriptivo para mostrar en UI / logs.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Si está activo, se incluye en la sincronización periódica.</summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Última vez que se ejecutó la sincronización de actividades del SIAT
        /// usando este (sucursal, puntoVenta). Sirve para auditoría.
        /// </summary>
        public DateTime? UltimaSyncActividades { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo de motivos
        /// de anulación del SIAT para este (sucursal, puntoVenta).
        /// </summary>
        public DateTime? UltimaSyncMotivoAnulacion { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización de la matriz
        /// Actividad ↔ Documento Sector del SIAT para este (sucursal, puntoVenta).
        /// </summary>
        public DateTime? UltimaSyncActividadesDocumentoSector { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo de leyendas
        /// obligatorias del SIAT para este (sucursal, puntoVenta).
        /// </summary>
        public DateTime? UltimaSyncLeyendas { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo de
        /// productos/servicios del SIAT para este (sucursal, puntoVenta).
        /// Alimenta la tabla <c>CodigosSiat</c> que consume el modal
        /// <c>CodigoSinModal</c> del frontend.
        /// </summary>
        public DateTime? UltimaSyncCodigosSiat { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo paramétrico
        /// de eventos significativos del SIAT para este (sucursal, puntoVenta).
        /// Alimenta la tabla <c>CatEventosSignificativos</c> (7 códigos: 1..7)
        /// que se usará cuando se implemente el flujo de contingencia.
        /// </summary>
        public DateTime? UltimaSyncEventosSignificativos { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo paramétrico de
        /// países de origen del SIAT para este (sucursal, puntoVenta). Alimenta la
        /// tabla <c>CatPaisesOrigen</c> (~211 códigos: 1..211) que se usará cuando
        /// se implemente el flujo de factura de exportación o clientes extranjeros.
        /// </summary>
        public DateTime? UltimaSyncPaisOrigen { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo paramétrico
        /// de tipos de documento de identidad del SIAT para este
        /// (sucursal, puntoVenta). Alimenta la tabla <c>CatTiposDocumentoIdentidad</c>
        /// (1=CI, 2=CEX, 3=PAS, 4=OD, 5=NIT según catálogo SIN vigente) que se
        /// usa para validar <c>codigoTipoDocumentoIdentidad</c> en cada venta
        /// facturada.
        /// </summary>
        public DateTime? UltimaSyncTipoDocumentoIdentidad { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo paramétrico de
        /// tipos de emisión del SIAT para este (sucursal, puntoVenta). Alimenta
        /// la tabla <c>CatTiposEmision</c> (1=EN LINEA, 2=FUERA DE LINEA,
        /// 3=MASIVO, 4=CONTINGENCIA según catálogo SIN vigente) y refresca el
        /// caché estático <c>TipoEmisionSiatCatalogo</c>. Sirve como auditoría
        /// para confirmar que el valor hardcoded <c>SiatOptions.CodigoEmision</c>
        /// sigue siendo oficial.
        /// </summary>
        public DateTime? UltimaSyncTipoEmision { get; set; }

        /// <summary>
        /// Última vez que se ejecutó la sincronización del catálogo paramétrico de
        /// tipos de método de pago del SIAT para este (sucursal, puntoVenta).
        /// Alimenta la tabla <c>CatMetodosPago</c> (~308 entradas: métodos simples
        /// 1..9 + combinaciones 10..308) y refresca el caché estático
        /// <c>MetodoPagoSiatCatalogo</c> usado por la validación de
        /// <c>DtoPagos</c>. NO corre diario (solo al boot + manual) — el catálogo
        /// cambia muy poco.
        /// </summary>
        public DateTime? UltimaSyncMetodoPago { get; set; }
    }
}