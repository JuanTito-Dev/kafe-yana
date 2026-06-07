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
    }

}
