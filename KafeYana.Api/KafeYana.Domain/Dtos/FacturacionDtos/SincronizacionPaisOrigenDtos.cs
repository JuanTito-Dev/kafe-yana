using System.Collections.Generic;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un país de origen devuelto por el SIAT en la respuesta de
    /// <c>sincronizarParametricaPaisOrigen</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los operadores, no se filtra por CAEB.
    /// </summary>
    public class PaisOrigenSiatDto
    {
        /// <summary>Código numérico del país (1..211 según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial del SIN (ej. "BOLIVIA (ESTADO PLURINACIONAL DE)").</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP <c>sincronizarParametricaPaisOrigen</c>.
    /// </summary>
    public class SincronizarPaisOrigenResponse
    {
        public bool Transaccion { get; set; }
        public List<PaisOrigenSiatDto> PaisesOrigen { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}