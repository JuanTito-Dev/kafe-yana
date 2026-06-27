namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un tipo de documento de identidad devuelto por el SIAT en la respuesta de
    /// <c>sincronizarParametricaTipoDocumentoIdentidad</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por CAEB.
    /// </summary>
    public class TipoDocumentoIdentidadSiatDto
    {
        /// <summary>Código numérico (1..N) que el SIN publica en su catálogo vigente.</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial (ej. "CI - CEDULA DE IDENTIDAD").</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP <c>sincronizarParametricaTipoDocumentoIdentidad</c>.
    /// </summary>
    public class SincronizarTipoDocumentoIdentidadResponse
    {
        public bool Transaccion { get; set; }
        public List<TipoDocumentoIdentidadSiatDto> TiposDocumentoIdentidad { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}