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

        /// <summary>
        /// Obligatorio SOLO cuando <see cref="CodigoEmision"/> = 2 (Contingencia computarizada).
        /// Contiene el <c>codigoRecepcionEventoSignificativo</c> que el SIN devolvió
        /// al registrar el evento bajo el cual se emitió esta nota. El SIAT lo
        /// cruza contra su log de eventos; si no coincide, rechaza.
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        public string? CodigoRecepcionEventoSignificativo { get; set; }
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
    /// notaComputarizadaCreditoDebito.xsd y previenen rechazos SIAT 1029/1030/1031/1049):
    ///
    /// 1. <see cref="IdVenta"/> debe corresponder a una venta con
    ///    <c>EstadoSiat == Validada (908)</c>. Otros estados devuelven 400.
    /// 2. <see cref="Detalles"/> representa los PRODUCTOS seleccionados por el
    ///    cajero para devolver (no las líneas SIAT finales). El servicio de
    ///    envío expande cada producto en un PAR canónico SIAT:
    ///       - línea trans=1: referencia al item original (cantidad=1, subTotal=precio original)
    ///       - línea trans=2: devolución efectiva (cantidad devuelta, subTotal=cant*precioUnitario)
    ///    Por eso N productos en la entrada ⇒ 2N líneas en el XML final.
    ///    Seleccionar al menos 1 producto cumple el mínimo XSD (minOccurs=2 en detalle).
    /// 3. Cada <see cref="DtoNotaAjusteDetalle"/> del body DEBE tener
    ///    <c>CodigoDetalleTransaccion == 1</c> (marcador semántico de producto
    ///    a devolver). El backend rechaza con 400 cualquier otro valor; el par
    ///    trans=2 lo genera el servicio de envío.
    /// 4. Cada <see cref="DtoNotaAjusteDetalle.IdDetallePagoOriginal"/> debe
    ///    corresponder a una línea real de la venta original; si no, 400.
    /// 5. <see cref="DtoNotaAjusteDetalle.Cantidad"/> no puede superar la
    ///    cantidad facturada del item original; si no, 400.
    /// </summary>
    public class DtoCrearNotaAjuste
    {
        public int IdVenta { get; set; }

        /// <summary>1=Devolución, 2=Descuento, 3=Corrección, 4=Otros.</summary>
        public int CodigoMotivoAjuste { get; set; }

        /// <summary>Descuento global aplicado a la nota (opcional).</summary>
        public decimal? MontoDescuentoCreditoDebito { get; set; }

        /// <summary>
        /// Descuento adicional sobre la factura original. OBLIGATORIO para sector 47
        /// (XSD <c>notaComputarizadaCreditoDebitoDescuento.xsd</c>): si se omite, el
        /// backend asume 0. Ignorado para sector 24 (la raíz
        /// <c>notaFiscalComputarizadaCreditoDebito</c> no lo incluye).
        /// </summary>
        public decimal? DescuentoAdicional { get; set; }

        /// <summary>Usuario que emite la nota (opcional — si null, se usa el del token).</summary>
        public string? Usuario { get; set; }

        /// <summary>Productos seleccionados por el cajero. El backend expande cada uno en par SIAT.</summary>
        public List<DtoNotaAjusteDetalle> Detalles { get; set; } = new();
    }

    /// <summary>
    /// Línea de detalle de la Nota de Crédito/Débito — entrada del body.
    ///
    /// El body NO contiene las líneas SIAT finales: cada producto seleccionado
    /// se traduce en el backend a un PAR (trans=1 + trans=2). Por eso el
    /// frontend siempre envía <see cref="CodigoDetalleTransaccion"/> = 1.
    ///
    /// Campos obligatorios por XSD (validación anti-rechazo SIAT):
    /// - <see cref="IdDetallePagoOriginal"/>: FK a una línea real de la venta original.
    ///   El backend rechaza con 400 si la línea no pertenece a la venta.
    /// - <see cref="Cantidad"/>: cantidad devuelta. El backend valida que no exceda
    ///   la cantidad facturada del item original.
    /// - <see cref="CodigoDetalleTransaccion"/>: SIEMPRE 1 en el body (marcador
    ///   semántico). El backend genera el trans=2 complementario.
    /// </summary>
    public class DtoNotaAjusteDetalle
    {
        /// <summary>FK a la línea original de la Venta que se ajusta (Id del Detalle_Pago).</summary>
        public int IdDetallePagoOriginal { get; set; }

        /// <summary>Marcador semántico. El frontend siempre envía 1 (Devolución);
        /// el backend genera el trans=2 complementario.</summary>
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

    /// <summary>
    /// Resumen de NotaAjuste para listas (ej: "GET /api/NotaAjuste/por-venta/{id}").
    /// Proyección delgada de <c>NotaAjuste</c> sin detalles ni XML pesado,
    /// apta para mostrar badges y montos en listados sin cargar el grafo completo.
    /// </summary>
    public class DtoNotaAjusteResumen
    {
        public int Id { get; set; }
        public int IdVenta { get; set; }
        public long NumeroNotaCreditoDebito { get; set; }
        public string? Cuf { get; set; }
        public string? EstadoSiat { get; set; }
        public string? CodigoRecepcion { get; set; }
        public int CodigoMotivoAjuste { get; set; }
        public DateTime FechaEmision { get; set; }
        public decimal MontoTotalOriginal { get; set; }
        public decimal MontoTotalDevuelto { get; set; }
        public decimal MontoEfectivoCreditoDebito { get; set; }
        /// <summary>
        /// True si la anulación de esta nota ya fue revertida en el SIAT.
        /// El SIAT solo permite revertir una vez; tras revertir, la nota
        /// vuelve a estado Validada pero NO puede volver a anularse.
        /// </summary>
        public bool RevertidaAnulacion { get; set; }
    }
}
