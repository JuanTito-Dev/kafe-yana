using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un documento sectorial devuelto por el SIAT
    /// en la respuesta de "sincronizarParametricaTipoDocumentoSector".
    /// <see cref="CodigoClasificador"/> es el valor que se envía en
    /// <c>&lt;codigoDocumentoSector&gt;</c> dentro del XML de la factura.
    /// </summary>
    public class DocumentoSectorSiatDto
    {
        /// <summary>Código numérico (ej. 1 = Factura Compra-Venta, 24 = Nota Crédito-Débito).</summary>
        public int CodigoClasificador { get; set; }

        /// <summary>Descripción legible (ej. "FACTURA COMPRA-VENTA").</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP "sincronizarParametricaTipoDocumentoSector".
    /// </summary>
    public class SincronizarDocumentosSectorResponse
    {
        public bool Transaccion { get; set; }
        public List<DocumentoSectorSiatDto> DocumentosSector { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}