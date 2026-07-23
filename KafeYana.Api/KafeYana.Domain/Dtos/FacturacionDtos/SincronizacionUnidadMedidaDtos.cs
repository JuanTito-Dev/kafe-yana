namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Una unidad de medida devuelta por el SIAT en la respuesta de
    /// <c>sincronizarParametricaUnidadMedida</c>.
    /// </summary>
    public class UnidadMedidaSiatDto
    {
        /// <summary>Código numérico SIN de la unidad de medida (ej. 57=UNIDAD, 28=LITRO).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN.</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta parseada de <c>sincronizarParametricaUnidadMedida</c>.
    ///
    /// Shape XML esperado:
    /// <code>
    /// &lt;RespuestaListaParametricas&gt;
    ///   &lt;transaccion&gt;true&lt;/transaccion&gt;
    ///   &lt;listaCodigos&gt;
    ///     &lt;codigoClasificador&gt;57&lt;/codigoClasificador&gt;
    ///     &lt;descripcion&gt;UNIDAD&lt;/descripcion&gt;
    ///   &lt;/listaCodigos&gt;
    ///   &lt;!-- N veces --&gt;
    /// &lt;/RespuestaListaParametricas&gt;
    /// </code>
    /// </summary>
    public class SincronizarParametricaUnidadMedidaResponse
    {
        public bool Transaccion { get; set; }

        /// <summary>Lista completa de unidades de medida del SIN (~50–100 entradas).</summary>
        public List<UnidadMedidaSiatDto> Unidades { get; set; } = new();

        /// <summary>Códigos de respuesta SIAT (vacío si transaccion=true).</summary>
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}