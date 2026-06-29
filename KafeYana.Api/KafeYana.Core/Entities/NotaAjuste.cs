using HotChocolate;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Nota de Crédito / Débito computarizada (SIAT — sector 24, tipoFacturaDocumento 3).
    /// Documento paralelo a una Venta validada; la Venta original no se modifica.
    /// </summary>
    public class NotaAjuste : BaseEntity
    {
        // --- Cabecera SIAT — obligatorios ---

        public long NitEmisor { get; set; }

        public required string RazonSocialEmisor { get; set; }

        public required string Municipio { get; set; }

        public int CodigoSucursal { get; set; }

        public required string Direccion { get; set; }

        public int CodigoPuntoVenta { get; set; }

        /// <summary>Correlativo propio de la nota (separado del NumeroFactura de la Venta).</summary>
        public long NumeroNotaCreditoDebito { get; set; }

        public required string Cuf { get; set; }

        public required string Cufd { get; set; }

        /// <summary>Fecha/hora UTC de emisión de la nota.</summary>
        public DateTime FechaEmision { get; set; }

        public int CodigoTipoDocumentoIdentidad { get; set; }

        public required string NumeroDocumento { get; set; }

        public required string CodigoCliente { get; set; }

        public int CodigoDocumentoSector { get; set; } = 24;

        public required string Leyenda { get; set; }

        public required string Usuario { get; set; }

        // --- Cabecera SIAT — opcionales ---

        public string? Telefono { get; set; }

        public string? NombreRazonSocial { get; set; }

        /// <summary>Opcional SEGIP. Si es null, en XML: complemento xsi:nil="true".</summary>
        public string? Complemento { get; set; }

        [GraphQLIgnore]
        public bool ComplementoEsNuloSiat => string.IsNullOrWhiteSpace(Complemento);

        public int? CodigoExcepcion { get; set; }

        // --- Referencia a la factura original (cabecera de la nota) ---

        /// <summary>NumeroFactura de la Venta que se ajusta.</summary>
        public long NumeroFacturaOriginal { get; set; }

        /// <summary>CUF de la Venta que se ajusta (numeroAutorizacionCuf en XSD).</summary>
        public required string NumeroAutorizacionCuf { get; set; }

        public DateTime FechaEmisionFactura { get; set; }

        // --- Montos de la nota ---

        public decimal MontoTotalOriginal { get; set; }

        /// <summary>
        /// Descuento adicional sobre la factura original. Obligatorio para sector 47
        /// (NCDDE — Nota Débito por Devolución / Descuentos Posteriores); null/sin
        /// serializar para sector 24 (NCD genérico).
        /// Referencia XSD: <c>notaComputarizadaCreditoDebitoDescuento.xsd</c>.
        /// </summary>
        public decimal? DescuentoAdicional { get; set; }

        public decimal MontoTotalDevuelto { get; set; }

        public decimal MontoDescuentoCreditoDebito { get; set; }

        public decimal MontoEfectivoCreditoDebito { get; set; }

        // --- Catálogo ---

        /// <summary>Motivo de ajuste (1=Devolución, 2=Descuento, etc.). No se serializa al XML — se conserva solo en BD para trazabilidad.</summary>
        public int CodigoMotivoAjuste { get; set; }

        // --- Proceso de recepción SIAT ---

        public int? TipoEmision { get; set; }

        public FacturaEstado? EstadoSiat { get; set; }

        public bool RevertidaAnulacion { get; set; }

        /// <summary>
        /// Fecha/hora UTC en la que el SIAT confirmó la anulación de esta nota
        /// (operación <c>anulacionDocumentoAjuste</c>). Null hasta que se anule.
        /// Sirve para auditoría y para distinguir "anulada antes" vs "nunca anulada".
        /// </summary>
        public DateTime? FechaAnulacionSiat { get; set; }

        public string? CodigoRecepcion { get; set; }

        public string? ErrorMensaje { get; set; }

        public string? CodigoHash { get; set; }

        public string? XmlBase64 { get; set; }

        // --- FK al evento de contingencia (cuando TipoEmision=2) ---

        /// <summary>
        /// FK al <c>EventoSignificativoSiat</c> bajo el cual se emitió esta nota
        /// cuando fue diferida por contingencia. Null para notas online (TipoEmision=1).
        /// Usado por el reenvío batch al recuperar SIAT. Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        public int? EventoSignificativoSiatId { get; set; }

        public EventoSignificativoSiat? EventoSignificativoSiat { get; set; }

        // --- FK a la Venta ajustada ---

        public int IdVenta { get; set; }

        public Venta? Venta { get; set; }

        public List<NotaAjusteDetalle> Detalles { get; set; } = new();
    }
}
