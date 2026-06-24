using System;
using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Sobre SOAP para la operación SIAT "recepcionDocumentoAjuste".
    /// IMPORTANTE: NO incluye Cufd — verificado contra el sobre de muestra del piloto
    /// (scripts/soap_recepcionDocumentoAjuste.xml). Diferencia intencional con la operación
    /// "recepcionFactura", que sí lo incluye.
    /// </summary>
    public class SolicitudRecepcionNotaAjusteDto
    {
        public int CodigoAmbiente { get; set; }
        public int CodigoDocumentoSector { get; set; }
        public int CodigoEmision { get; set; }
        public int CodigoModalidad { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public int CodigoSucursal { get; set; }
        public string Cuis { get; set; } = string.Empty;
        public long Nit { get; set; }
        public int TipoFacturaDocumento { get; set; } = 3;
        public string Archivo { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public string HashArchivo { get; set; } = string.Empty;
    }

    public class RespuestaRecepcionNotaAjusteDto
    {
        public bool Transaccion { get; set; }
        public int? CodigoEstado { get; set; }
        public string? CodigoRecepcion { get; set; }
        public string? CodigoDescripcion { get; set; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }

    /// <summary>
    /// Entrada del POST /api/NotaAjuste.
    ///
    /// REGLAS DE NEGOCIO NO NEGOCIABLES (replican restricciones del XSD
    /// notaComputarizadaCreditoDebito.xsd y previenen rechazos SIAT 1031/1029):
    ///
    /// 1. <see cref="IdVenta"/> debe corresponder a una venta con
    ///    <c>EstadoSiat == Validada (908)</c>. Otros estados devuelven 400.
    /// 2. <see cref="Detalles"/> DEBE contener mínimo 2 elementos.
    ///    - No es burocracia: el XSD exige al menos 2 eventos de ajuste por nota.
    ///    - Si en el negocio solo hay 1 producto a devolver, la UI debe agregar
    ///      explícitamente una 2da línea técnica (SubTotal=0.01,
    ///      CodigoDetalleTransaccion=3 'Ajuste manual') con el consentimiento
    ///      del cajero. La inyección silenciosa desde el frontend está prohibida
    ///      porque rompe la auditoría fiscal y suele ser rechazada por el SIAT.
    /// 3. La suma de <see cref="DtoNotaAjusteDetalle.SubTotal"/> de los detalles
    ///    DEBE coincidir exactamente con el monto total devuelto al cliente
    ///    (tolerancia ±0.01 por redondeo decimal). Si no, se lanza
    ///    <c>InvalidOperationException</c> con código 400 antes de enviar al SIAT.
    /// 4. Cada <see cref="DtoNotaAjusteDetalle.IdDetallePagoOriginal"/> debe
    ///    corresponder a una línea real de la venta original; si no, 400.
    /// </summary>
    public class DtoCrearNotaAjuste
    {
        public int IdVenta { get; set; }

        /// <summary>1=Devolución, 2=Descuento, 3=Corrección, 4=Otros.</summary>
        public int CodigoMotivoAjuste { get; set; }

        /// <summary>Descuento global aplicado a la nota (opcional).</summary>
        public decimal? MontoDescuentoCreditoDebito { get; set; }

        /// <summary>Usuario que emite la nota (opcional — si null, se usa el del token).</summary>
        public string? Usuario { get; set; }

        /// <summary>Mínimo 2 elementos (ver regla #2 en el summary de la clase).</summary>
        public List<DtoNotaAjusteDetalle> Detalles { get; set; } = new();
    }

    /// <summary>
    /// Línea de detalle de la Nota de Crédito/Débito.
    ///
    /// Campos obligatorios por XSD (validación anti-rechazo SIAT):
    /// - <see cref="IdDetallePagoOriginal"/>: FK a una línea real de la venta original.
    ///   El backend rechaza con 400 si la línea no pertenece a la venta.
    /// - <see cref="NumeroLineaOriginal"/>: posición (1, 2, ...) de la línea original.
    /// - <see cref="CodigoDetalleTransaccion"/>: 1=Devolución, 2=Descuento, 3=Ajuste técnico.
    /// </summary>
    public class DtoNotaAjusteDetalle
    {
        /// <summary>FK a la línea original de la Venta que se ajusta (Id del Detalle_Pago).</summary>
        public int IdDetallePagoOriginal { get; set; }

        /// <summary>1=Devolución, 2=Descuento, 3=Ajuste técnico.</summary>
        public int CodigoDetalleTransaccion { get; set; }

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? MontoDescuento { get; set; }
    }

    public sealed class ResultadoEnvioNotaAjusteSiatDto
    {
        public bool Enviado { get; init; }
        public bool Transaccion { get; init; }
        public int? NotaAjusteId { get; init; }
        public int? NumeroNotaCreditoDebito { get; init; }
        public string? Cuf { get; init; }
        public int? CodigoEstado { get; init; }
        public string? CodigoRecepcion { get; init; }
        public string? CodigoDescripcion { get; init; }
        public string? ErrorMensaje { get; init; }
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; init; } = new();
    }
}
