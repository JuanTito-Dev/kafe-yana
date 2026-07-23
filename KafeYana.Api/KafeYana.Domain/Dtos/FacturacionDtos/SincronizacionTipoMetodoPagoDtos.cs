namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un método de pago devuelto por el SIAT en la respuesta de
    /// <c>sincronizarParametricaTipoMetodoPago</c>.
    /// </summary>
    public class TipoMetodoPagoSiatDto
    {
        /// <summary>Código numérico SIN (1..308 según catálogo vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN.</summary>
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta parseada de <c>sincronizarParametricaTipoMetodoPago</c>.
    ///
    /// Shape XML esperado:
    /// <code>
    /// &lt;RespuestaListaParametricas&gt;
    ///   &lt;transaccion&gt;true&lt;/transaccion&gt;
    ///   &lt;listaCodigos&gt;
    ///     &lt;codigoClasificador&gt;1&lt;/codigoClasificador&gt;
    ///     &lt;descripcion&gt;EFECTIVO&lt;/descripcion&gt;
    ///   &lt;/listaCodigos&gt;
    ///   &lt;!-- N veces --&gt;
    /// &lt;/RespuestaListaParametricas&gt;
    /// </code>
    /// </summary>
    public class SincronizarTipoMetodoPagoResponse
    {
        public bool Transaccion { get; set; }

        /// <summary>Lista completa de métodos de pago del SIN (~308 entradas).</summary>
        public List<TipoMetodoPagoSiatDto> MetodosPago { get; set; } = new();

        /// <summary>Códigos de respuesta SIAT (vacío si transaccion=true).</summary>
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}