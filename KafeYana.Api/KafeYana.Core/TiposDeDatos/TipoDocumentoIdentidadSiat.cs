namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>Paramétrica SIAT codigoTipoDocumentoIdentidad (valores 1 a 5).</summary>
    public enum TipoDocumentoIdentidadSiat
    {
        CiCedulaIdentidad = 1,
        CexCedulaExtranjero = 2,
        PasPasaporte = 3,
        OdOtroDocumento = 4,
        Nit = 5
    }

    public static class TipoDocumentoIdentidadSiatDescripciones
    {
        public static readonly IReadOnlyDictionary<int, string> PorCodigo = new Dictionary<int, string>
        {
            [1] = "CI - CEDULA DE IDENTIDAD",
            [2] = "CEX - CEDULA DE IDENTIDAD DE EXTRANJERO",
            [3] = "PAS - PASAPORTE",
            [4] = "OD - OTRO DOCUMENTO DE IDENTIDAD",
            [5] = "NIT - NÚMERO DE IDENTIFICACIÓN TRIBUTARIA"
        };
    }
}
