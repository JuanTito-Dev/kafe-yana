namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>Paramétrica SIAT codigoMotivo para anulación de factura y nota C/D.</summary>
    public enum MotivoAnulacionSiat
    {
        FacturaMalEmitida = 1,
        NotaCreditoDebito = 2,
        DatosClienteIncorrectos = 3,
        Otros = 4
    }

    /// <summary>
    /// Catálogo de motivos de anulación leído desde la BD (tabla CatMotivosAnulacion,
    /// sincronizada por SincronizadorCatMotivoAnulacion).
    ///
    /// Se mantiene un caché en memoria (inmutable por reemplazo atómico) para no
    /// golpear la BD en cada anulación. El caché arranca con descripciones
    /// hardcoded como fallback mientras el primer sync del SIAT no haya corrido;
    /// apenas el sync termina, llama a <see cref="Refrescar"/> y el sistema pasa a
    /// usar las descripciones oficiales del SIN.
    ///
    /// Es thread-safe: el reemplazo del diccionario es atómico y los lectores
    /// siempre ven una versión consistente (vieja o nueva, nunca mixta).
    /// </summary>
    public static class MotivoAnulacionSiatCatalogo
    {
        // IMPORTANTE: declarar FallbackHardcoded ANTES de _cache.
        // C# ejecuta los field initializers estáticos en orden textual.
        // Si _cache se declara primero y referencia FallbackHardcoded,
        // cuando corre su initializer FallbackHardcoded todavía es null
        // (default de reference type) y _cache queda apuntando a null
        // hasta el primer Refrescar() exitoso. Si el sync falla al boot
        // (típico: SIAT caído), _cache permanece null para siempre y la
        // primera anulación NRE en EsValido. Ver
        // [[kafeyana-catalogo-typeinit-duplicate-keys]] para el bug original
        // (ToDictionary con duplicados); este es el primo hermano: forward
        // reference entre static fields.
        private static readonly IReadOnlyDictionary<int, string> FallbackHardcoded =
            new Dictionary<int, string>
            {
                [1] = "Factura mal emitida",
                [2] = "Nota de crédito/débito",
                [3] = "Datos del cliente incorrectos",
                [4] = "Otros",
            };

        // Snapshot inmutable de los motivos conocidos. Se reemplaza atómicamente
        // vía Interlocked.Exchange para que los lectores vean una versión estable.
        private static volatile IReadOnlyDictionary<int, string> _cache = FallbackHardcoded;

        /// <summary>
        /// True mientras el caché contenga los valores de <see cref="FallbackHardcoded"/>
        /// (server arrancó pero ningún sync del SIAT corrió todavía). Pasa a false
        /// en cuanto <see cref="Refrescar"/> recibe motivos válidos del SIN.
        ///
        /// Útil para que la UI pueda mostrar un aviso de "catálogo no sincronizado"
        /// en lugar de presentar el fallback como si fuera oficial.
        /// </summary>
        public static bool EsFallback { get; private set; } = true;

        /// <summary>True si el código está en el catálogo vigente (BD o fallback).</summary>
        public static bool EsValido(int codigo) => _cache.ContainsKey(codigo);

        /// <summary>
        /// Descripción del motivo. Si no existe en el catálogo, devuelve
        /// "Motivo {codigo}" en lugar de lanzar para no romper respuestas al cliente.
        /// </summary>
        public static string ObtenerDescripcion(int codigo) =>
            _cache.TryGetValue(codigo, out var d) ? d : $"Motivo {codigo}";

        /// <summary>
        /// Snapshot de solo-lectura del catálogo actual. Útil para la UI
        /// (lista de opciones del dropdown).
        /// </summary>
        public static IReadOnlyDictionary<int, string> ObtenerTodos() => _cache;

        /// <summary>
        /// Llamado por <c>SincronizadorCatMotivoAnulacion</c> al terminar una
        /// sync exitosa. Reemplaza el caché atómicamente con los motivos del SIN.
        /// </summary>
        public static void Refrescar(IEnumerable<(int Codigo, string Descripcion)> motivos)
        {
            if (motivos is null) return;

            var nuevo = motivos
                .Where(m => m.Codigo > 0 && !string.IsNullOrWhiteSpace(m.Descripcion))
                .GroupBy(m => m.Codigo)
                .ToDictionary(g => g.Key, g => g.First().Descripcion.Trim());

            if (nuevo.Count == 0) return;

            // Reemplazo atómico: cualquier lector en vuelo verá el diccionario viejo
            // o el nuevo, nunca uno parcial.
            Interlocked.Exchange(ref _cache, nuevo);
            EsFallback = false;
        }
    }
}
