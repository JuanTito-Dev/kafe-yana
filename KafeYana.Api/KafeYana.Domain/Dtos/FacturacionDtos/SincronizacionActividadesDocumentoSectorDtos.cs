using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Una fila de la matriz Actividad ↔ Documento Sector devuelta por el SIAT
    /// en la respuesta de "sincronizarListaActividadesDocumentoSector".
    /// </summary>
    public class ActividadDocumentoSectorSiatDto
    {
        /// <summary>Código CAEB (ej. "4630600").</summary>
        public string CodigoActividad { get; set; } = string.Empty;

        /// <summary>Código de documento sector (1 = FCV, 24 = NCD, 35 = FAC_CVB, 47 = NCDDE).</summary>
        public int CodigoDocumentoSector { get; set; }

        /// <summary>Tipo de documento sector (FCV, NCD, NCDDE, FAC_CVB, …).</summary>
        public string TipoDocumentoSector { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP
    /// "sincronizarListaActividadesDocumentoSector".
    /// </summary>
    public class SincronizarActividadesDocumentoSectorResponse
    {
        public bool Transaccion { get; set; }
        public List<ActividadDocumentoSectorSiatDto> ActividadesDocumentoSector { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}