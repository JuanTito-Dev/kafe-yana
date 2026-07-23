using HotChocolate;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Domain.Entities
{
    /// <summary>Cabecera de Factura Compra y Venta (SIAT) + trazabilidad de recepción.</summary>
    public class Venta : BaseEntity
    {
        // --- Cabecera SIAT — obligatorios ---

        public long NitEmisor { get; set; }

        public required string RazonSocialEmisor { get; set; }

        public required string Municipio { get; set; }

        public long? NumeroFactura { get; set; }

        /// <summary>True cuando la venta fue emitida como factura electrónica (SIAT).</summary>
        public bool Facturado { get; set; }

        public required string Cuf { get; set; }

        public required string Cufd { get; set; }

        public int CodigoSucursal { get; set; }

        public required string Direccion { get; set; }

        /// <summary>Fecha/hora UTC de emisión.</summary>
        public DateTime FechaEmision { get; set; }

        public int CodigoTipoDocumentoIdentidad { get; set; }

        public required string NumeroDocumento { get; set; }

        public required string CodigoCliente { get; set; }

        public int CodigoMetodoPago { get; set; }

        public decimal MontoTotal { get; set; }

        public decimal MontoTotalSujetoIva { get; set; }

        public int CodigoMoneda { get; set; }

        public decimal TipoCambio { get; set; }

        public decimal MontoTotalMoneda { get; set; }

        public required string Leyenda { get; set; }

        public required string Usuario { get; set; }

        public int CodigoDocumentoSector { get; set; }

        // --- Cabecera SIAT — opcionales ---

        public string? Telefono { get; set; }

        /// <summary>Enviar 0 cuando no aplica punto de venta.</summary>
        public int CodigoPuntoVenta { get; set; }

        public string? NombreRazonSocial { get; set; }

        /// <summary>Opcional SEGIP. Si es null, en XML: complemento xsi:nil="true".</summary>
        public string? Complemento { get; set; }

        [GraphQLIgnore]
        public bool ComplementoEsNuloSiat => string.IsNullOrWhiteSpace(Complemento);

        public string? NumeroTarjeta { get; set; }

        public decimal? MontoGiftCard { get; set; }

        public decimal? DescuentoAdicional { get; set; }

        public int? CodigoExcepcion { get; set; }

        public string? Cafc { get; set; }

        // --- Proceso de recepción SIAT (se llenan al enviar la factura) ---

        public int? TipoEmision { get; set; }

        /// <summary>
        /// FK opcional al evento significativo SIAT bajo el cual se emitió esta factura.
        /// Null en línea; poblado (= 4) en contingencia. Permite JOIN al reenviar facturas
        /// pendientes y al auditar qué evento cobija cada factura emitida fuera de línea.
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        public int? EventoSignificativoSiatId { get; set; }

        [GraphQLIgnore]
        public EventoSignificativoSiat? EventoSignificativoSiat { get; set; }

        public FacturaEstado? EstadoSiat { get; set; }

        /// <summary>True si la anulación errónea ya fue revertida en el SIAT (solo permitido una vez).</summary>
        public bool RevertidaAnulacion { get; set; }

        public string? CodigoRecepcion { get; set; }

        public string? ErrorMensaje { get; set; }

        public string? CodigoHash { get; set; }

        public string? XmlBase64 { get; set; }

        public List<Detalle_Pago> Detalles { get; set; } = new List<Detalle_Pago>();

        /// <summary>
        /// Líneas de pago individuales (método + monto). Permite registrar pagos
        /// mixtos. Hoy KafeYana emite UN solo <c>codigoMetodoPago</c> en el XML
        /// al SIAT (el de mayor monto), pero persiste todas las líneas para
        /// auditoría y para futuros flujos.
        /// </summary>
        public List<VentaPago> Pagos { get; set; } = new List<VentaPago>();

        /// <summary>
        /// Notas de Crédito/Débito SIAT que ajustan esta venta (relación inversa del FK
        /// <c>NotaAjuste.IdVenta</c>). No requiere migración: EF infiere la FK del lado de
        /// <c>NotaAjuste</c> ya existente. HotChocolate la expone como <c>notasAjuste</c>
        /// en <c>VentaNode</c> para que la lista y el detalle lleguen con JOIN en una sola query.
        /// </summary>
        public List<NotaAjuste> NotasAjuste { get; set; } = new();

        public CajaMovimiento Reembolso(Caja caja, decimal monto, TipoPagos tipoPago, string motivo)
        {
            EstadoSiat = FacturaEstado.Anulada;
            return caja.RegistrarReembolso(monto, tipoPago, motivo, Cuf);
        }
    }
}
