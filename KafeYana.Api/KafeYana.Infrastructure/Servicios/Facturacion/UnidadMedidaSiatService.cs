using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Wrapper delgado sobre <see cref="UnidadMedidaSiatCatalogo"/> (caché
    /// estático en memoria alimentado por <c>SincronizadorCatUnidadMedida</c>).
    ///
    /// Antes del sync 12, este servicio tenía los 12 pares hardcoded como
    /// <c>Dictionary&lt;string, int&gt;</c>. Ahora todo viene del catálogo
    /// sincronizado contra el SIAT (los 12 hardcoded se siembran como
    /// <c>Activo=true</c>, los códigos adicionales del SIN se guardan en BD
    /// con <c>Activo=false</c>).
    ///
    /// Las firmas públicas se mantienen para no romper los 5 call-sites
    /// existentes (<c>ElaboradoController</c>, <c>ProductoController</c>,
    /// <c>VentaServices</c>, <c>FacturaVentaSiatPreparer</c>,
    /// <c>FacturaTicketBuilder</c>, <c>Detalle_RondaService</c>).
    /// </summary>
    public static class UnidadMedidaSiatService
    {
        /// <summary>
        /// True si el código está en el catálogo vigente Y está activo. Es el
        /// criterio que usan las validaciones de venta/preparer para rechazar
        /// unidades deshabilitadas.
        /// </summary>
        public static bool EsCodigoValido(int codigo) =>
            UnidadMedidaSiatCatalogo.EsValidoYActivo(codigo);

        /// <summary>
        /// Resuelve la descripción que el operador tipea (ej. "UNIDAD", "VASO")
        /// al código SIN correspondiente. Solo resuelve si la unidad está
        /// activa. Usado por los formularios de creación/edición de productos.
        /// </summary>
        public static bool TryResolver(
            string descripcion,
            out int codigo,
            out string descripcionCanonica) =>
            UnidadMedidaSiatCatalogo.TryResolver(descripcion, out codigo, out descripcionCanonica);

        /// <summary>
        /// Lista de las unidades activas (las que la cafetería puede usar).
        /// Usado por <c>FacturaTicketBuilder</c> para imprimir la descripción
        /// de la unidad en el ticket.
        /// </summary>
        public static IReadOnlyList<UnidadMedidaSiatItem> Listar() =>
            UnidadMedidaSiatCatalogo.ObtenerActivos()
                .Select(x => new UnidadMedidaSiatItem(x.Key, x.Value))
                .ToList();
    }
}