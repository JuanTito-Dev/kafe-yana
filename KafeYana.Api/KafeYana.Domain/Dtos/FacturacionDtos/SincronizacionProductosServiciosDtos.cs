namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Un producto/servicio devuelto por el SIAT en la respuesta de
    /// <c>sincronizarListaProductosServicios</c>.
    ///
    /// El SIAT devuelve una entrada por cada (codigoActividad, codigoProducto):
    /// un mismo código de producto puede existir para múltiples actividades
    /// económicas (ej: "café" como insumo vs "café" como producto terminado),
    /// y el SIAT las publica como entradas independientes. KafeYana filtra
    /// por la actividad económica PRINCIPAL del operador antes de persistir.
    ///
    /// El SOAP también devuelve múltiples <c>&lt;nandina&gt;</c> hermanos por
    /// entrada (códigos aduaneros), pero este DTO NO los modela — la tabla
    /// <c>CodigosSiat</c> no tiene esa columna. Si en el futuro se necesita,
    /// es un ALTER TABLE + un List&lt;string&gt; Nandina acá.
    /// </summary>
    public class ProductoServicioSiatDto
    {
        /// <summary>Código CAEB al que aplica este producto (nvarchar en BD).</summary>
        public string CodigoActividad { get; set; } = string.Empty;

        /// <summary>Código de producto/servicio según el SIN (nvarchar en BD).</summary>
        public string CodigoProducto { get; set; } = string.Empty;

        /// <summary>Descripción oficial devuelta por el SIN (ej. "Café tostado...").</summary>
        public string DescripcionProducto { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de la operación SOAP "sincronizarListaProductosServicios".
    /// </summary>
    public class SincronizarProductosServiciosResponse
    {
        public bool Transaccion { get; set; }
        public List<ProductoServicioSiatDto> ProductosServicios { get; set; } = new();
        public List<CodigoRespuestaSiatDto> CodigosRespuesta { get; set; } = new();
    }
}