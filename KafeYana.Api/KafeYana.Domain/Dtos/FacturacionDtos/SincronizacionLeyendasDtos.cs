namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Una leyenda devuelta por el SIAT en la respuesta de
    /// <c>sincronizarListaLeyendasFactura</c>.
    ///
    /// El SIAT devuelve una leyenda por cada (codigoActividad, descripcionLeyenda):
    /// una misma actividad puede tener varias leyendas (las distintas cláusulas
    /// de la Ley 453, por ejemplo) y el SIAT las publica como entradas
    /// independientes. KafeYana filtra por la actividad económica principal
    /// del operador antes de persistir.
    /// </summary>
    public class LeyendaSiatDto
    {
        /// <summary>Código CAEB al que aplica esta leyenda (nvarchar en BD).</summary>
        public string CodigoActividad { get; set; } = string.Empty;

        /// <summary>Texto oficial devuelto por el SIN (ej. "Ley N° 453: ...").</summary>
        public string DescripcionLeyenda { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP "sincronizarListaLeyendasFactura".
    /// </summary>
    public class SincronizarLeyendasResponse
    {
        public bool Transaccion { get; set; }
        public List<LeyendaSiatDto> Leyendas { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}
