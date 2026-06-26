using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Configuration
{
    /// <summary>
    /// Mapeado desde appsettings.json sección "Siat"
    /// </summary>
    public class SiatOptions
    {
        public const string SeccionNombre = "Siat";

        /// <summary>
        /// URL base del servicio.
        /// Piloto:     https://pilotosiatservicios.impuestos.gob.bo/v2
        /// Producción: https://siatservicios.impuestos.gob.bo/v2
        /// </summary>
        public string UrlBase { get; set; } = string.Empty;

        /// <summary>
        /// Header: apikey → "TokenApi eyJ0eX..."
        /// Se obtiene desde el portal SIAT al autorizar el sistema.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Código del sistema autorizado por el SIN.
        /// Ej: 227B0024E9619DE80AD0E
        /// </summary>
        public string CodigoSistema { get; set; } = string.Empty;

        /// <summary>NIT del contribuyente (la cafetería).</summary>
        public long Nit { get; set; }

        /// <summary>
        /// 1 = Producción, 2 = Piloto/Pruebas
        /// </summary>
        public int CodigoAmbiente { get; set; } = 2;

        /// <summary>
        /// 1 = Electrónica en Línea, 2 = Computarizada en Línea
        /// </summary>
        public int CodigoModalidad { get; set; } = 2;

        /// <summary>Casa Matriz = 0, Sucursales = 1,2,...n</summary>
        public int CodigoSucursal { get; set; } = 0;

        /// <summary>Punto de venta = 0 si no aplica, 1,2,...n si hay varios</summary>
        public int CodigoPuntoVenta { get; set; } = 0;

        /// <summary>Timeout en segundos para llamadas al SIAT.</summary>
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>Sector de documento. Compra y Venta = 1.</summary>
        public int CodigoDocumentoSector { get; set; } = 1;

        /// <summary>Tipo de factura documento. Compra y Venta = 1.</summary>
        public int TipoFacturaDocumento { get; set; } = 1;

        /// <summary>Emisión en línea = 1.</summary>
        public int CodigoEmision { get; set; } = 1;

        /// <summary>
        /// Servicio SOAP de recepción según sector.
        /// Compra y Venta: ServicioFacturacionCompraVenta
        /// </summary>
        public string ServicioRecepcionFactura { get; set; } = "ServicioFacturacionCompraVenta";

        /// <summary>
        /// Servicio SOAP de anulación según sector.
        /// Compra y Venta: ServicioFacturacionCompraVenta
        /// </summary>
        public string ServicioAnulacionFactura { get; set; } = "ServicioFacturacionCompraVenta";

        /// <summary>
        /// Servicio SOAP de reversión de anulación según sector.
        /// Compra y Venta: ServicioFacturacionCompraVenta
        /// </summary>
        public string ServicioReversionAnulacionFactura { get; set; } = "ServicioFacturacionCompraVenta";

        /// <summary>
        /// Servicio SOAP para notas de crédito/débito computarizadas.
        /// Piloto: ServicioFacturacionDocumentoAjuste (distinto de CompraVenta).
        /// </summary>
        public string ServicioRecepcionNotaAjuste { get; set; } = "ServicioFacturacionDocumentoAjuste";

        /// <summary>
        /// Servicio SOAP de anulación de nota C/D.
        /// Mismo que recepción: ServicioFacturacionDocumentoAjuste.
        /// </summary>
        public string ServicioAnulacionNotaAjuste { get; set; } = "ServicioFacturacionDocumentoAjuste";

        /// <summary>
        /// Servicio SOAP de reversión de anulación de nota C/D.
        /// Mismo que recepción: ServicioFacturacionDocumentoAjuste.
        /// </summary>
        public string ServicioReversionAnulacionNotaAjuste { get; set; } = "ServicioFacturacionDocumentoAjuste";

        /// <summary>Sector SIAT para notas de crédito/débito (24 por defecto).</summary>
        public int CodigoDocumentoSectorNotaAjuste { get; set; } = 24;

        /// <summary>Tipo de documento para notas de crédito/débito (3 por defecto).</summary>
        public int TipoFacturaDocumentoNotaAjuste { get; set; } = 3;

        /// <summary>0 por defecto. 1 solo si el SIN autoriza factura a NIT inválido.</summary>
        public int CodigoExcepcion { get; set; } = 0;

        /// <summary>CAFC de contingencia. Vacío si no aplica.</summary>
        public string Cafc { get; set; } = string.Empty;

        /// <summary>1 = Boliviano.</summary>
        public int CodigoMoneda { get; set; } = 1;

        /// <summary>1 cuando la moneda es boliviano.</summary>
        public decimal TipoCambio { get; set; } = 1;
    }

}
