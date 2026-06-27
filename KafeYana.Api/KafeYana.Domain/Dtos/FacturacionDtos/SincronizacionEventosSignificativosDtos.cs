using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un evento significativo devuelto por el SIAT en la respuesta de
    /// <c>sincronizarParametricaEventosSignificativos</c>.
    ///
    /// El catálogo es UNIVERSAL: aplica a todos los operadores y actividades
    /// económicas, no se filtra por CAEB (a diferencia de productos o leyendas).
    /// </summary>
    public class EventoSignificativoSiatDto
    {
        /// <summary>Código numérico del evento (1..7 según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial del SIN.</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP <c>sincronizarParametricaEventosSignificativos</c>.
    /// </summary>
    public class SincronizarEventosSignificativosResponse
    {
        public bool Transaccion { get; set; }
        public List<EventoSignificativoSiatDto> EventosSignificativos { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}
