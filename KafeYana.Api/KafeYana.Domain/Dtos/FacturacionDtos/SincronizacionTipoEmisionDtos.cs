namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un tipo de emisión devuelto por el SIAT en la respuesta de
    /// <c>sincronizarParametricaTipoEmision</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por CAEB.
    /// </summary>
    public class TipoEmisionSiatDto
    {
        /// <summary>Código numérico (1..N) que el SIN publica en su catálogo vigente.</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial (ej. "EN LINEA", "FUERA DE LINEA", "MASIVO", "CONTINGENCIA").</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP <c>sincronizarParametricaTipoEmision</c>.
    /// </summary>
    public class SincronizarTipoEmisionResponse
    {
        public bool Transaccion { get; set; }
        public List<TipoEmisionSiatDto> TiposEmision { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}
