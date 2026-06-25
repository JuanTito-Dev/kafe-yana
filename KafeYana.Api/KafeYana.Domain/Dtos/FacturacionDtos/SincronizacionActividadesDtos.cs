using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Una actividad económica (CAEB) devuelta por el SIAT
    /// en la respuesta de "sincronizarActividades".
    /// </summary>
    public class ActividadSiatDto
    {
        public string CodigoCaeb { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TipoActividad { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP "sincronizarActividades".
    /// </summary>
    public class SincronizarActividadesResponse
    {
        public bool Transaccion { get; set; }
        public List<ActividadSiatDto> Actividades { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}