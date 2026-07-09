using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>
    /// Catálogo de tipos de documento de identidad leído desde la BD
    /// (tabla <c>CatTiposDocumentoIdentidad</c>, sincronizada por
    /// <c>SincronizadorCatTipoDocumentoIdentidad</c>).
    ///
    /// Se mantiene un caché en memoria (inmutable por reemplazo atómico) para
    /// no golpear la BD en cada venta facturada. El caché arranca con descripciones
    /// hardcoded como fallback mientras el primer sync del SIAT no haya corrido;
    /// apenas el sync termina, llama a <see cref="Refrescar"/> y el sistema pasa
    /// a usar las descripciones oficiales del SIN.
    ///
    /// Es thread-safe: el reemplazo del diccionario es atómico y los lectores
    /// siempre ven una versión consistente (vieja o nueva, nunca mixta).
    ///
    /// Espejo de <see cref="MotivoAnulacionSiatCatalogo"/> (mismo patrón:
    /// catálogo paramétrico del SIAT leído en cada request de venta / anulación).
    /// </summary>
    public static class TipoDocumentoIdentidadSiatCatalogo
    {
        // IMPORTANTE: declarar FallbackHardcoded ANTES de _cache.
        // C# ejecuta los field initializers estáticos en orden textual.
        // Si _cache se declara primero y referencia FallbackHardcoded,
        // cuando corre su initializer FallbackHardcoded todavía es null
        // (default de reference type) y _cache queda apuntando a null
        // hasta el primer Refrescar() exitoso. Si el sync falla al boot
        // (típico: SIAT caído), _cache permanece null para siempre y el
        // primer cobro NRE en EsValido (vía DtoVentaPedido.Validate).
        // Ver [[kafeyana-catalogo-typeinit-duplicate-keys]] — el bug original
        // era ToDictionary con duplicados; este es el primo hermano: forward
        // reference entre static fields.
        private static readonly IReadOnlyDictionary<int, string> FallbackHardcoded =
            new Dictionary<int, string>
            {
                [1] = "CI - CEDULA DE IDENTIDAD",
                [2] = "CEX - CEDULA DE IDENTIDAD DE EXTRANJERO",
                [3] = "PAS - PASAPORTE",
                [4] = "OD - OTRO DOCUMENTO DE IDENTIDAD",
                [5] = "NIT - NUMERO DE IDENTIFICACION TRIBUTARIA",
            };

        // Snapshot inmutable de los tipos conocidos. Se reemplaza atómicamente
        // vía Interlocked.Exchange para que los lectores vean una versión estable.
        private static volatile IReadOnlyDictionary<int, string> _cache = FallbackHardcoded;

        /// <summary>
        /// True mientras el caché contenga los valores de <see cref="FallbackHardcoded"/>
        /// (server arrancó pero ningún sync del SIAT corrió todavía). Pasa a false
        /// en cuanto <see cref="Refrescar"/> recibe tipos válidos del SIN.
        ///
        /// Útil para que la UI pueda mostrar un aviso de "catálogo no sincronizado"
        /// en lugar de presentar el fallback como si fuera oficial.
        /// </summary>
        public static bool EsFallback { get; private set; } = true;

        /// <summary>
        /// True cuando el caché fue poblado desde <c>CatTiposDocumentoIdentidad</c> (última
        /// sync exitosa persistida en BD) en lugar de una respuesta en vivo del SIAT. Se usa
        /// cuando SIAT no responde pero hay datos previos utilizables — ver
        /// <see cref="CargarDesdeBaseDatos"/>. Vuelve a false apenas un sync en vivo tiene éxito.
        /// </summary>
        public static bool EsDesdeBaseDatos { get; private set; }

        /// <summary>True si el código está en el catálogo vigente (BD o fallback).</summary>
        public static bool EsValido(int codigo) => _cache.ContainsKey(codigo);

        /// <summary>
        /// Descripción del tipo de documento. Si no existe en el catálogo, devuelve
        /// "Tipo {codigo}" en lugar de lanzar para no romper respuestas al cliente.
        /// </summary>
        public static string ObtenerDescripcion(int codigo) =>
            _cache.TryGetValue(codigo, out var d) ? d : $"Tipo {codigo}";

        /// <summary>
        /// Snapshot de solo-lectura del catálogo actual. Útil para la UI
        /// (lista de opciones del dropdown de tipo de documento).
        /// </summary>
        public static IReadOnlyDictionary<int, string> ObtenerTodos() => _cache;

        /// <summary>
        /// Llamado por <c>SincronizadorCatTipoDocumentoIdentidad</c> al terminar una
        /// sync exitosa. Reemplaza el caché atómicamente con los tipos del SIN.
        /// </summary>
        public static void Refrescar(IEnumerable<(int Codigo, string Descripcion)> tipos)
        {
            var nuevo = Normalizar(tipos);
            if (nuevo is null) return;

            // Reemplazo atómico: cualquier lector en vuelo verá el diccionario viejo
            // o el nuevo, nunca uno parcial.
            Interlocked.Exchange(ref _cache, nuevo);
            EsFallback = false;
            EsDesdeBaseDatos = false;
        }

        /// <summary>
        /// Llamado cuando el SIAT no responde (boot o sync manual) pero
        /// <c>CatTiposDocumentoIdentidad</c> ya tiene datos de una sync anterior exitosa.
        /// Sirve ese último catálogo conocido en vez del fallback hardcodeado, sin límite
        /// de antigüedad: los códigos de tipo de documento del SIN cambian muy rara vez.
        /// No pisa el caché si el sync en vivo ya lo actualizó o si la BD está vacía
        /// (instalación nueva, nunca sincronizó — ahí se mantiene <see cref="FallbackHardcoded"/>).
        /// </summary>
        public static void CargarDesdeBaseDatos(IEnumerable<(int Codigo, string Descripcion)> tipos)
        {
            var nuevo = Normalizar(tipos);
            if (nuevo is null) return;

            Interlocked.Exchange(ref _cache, nuevo);
            EsFallback = false;
            EsDesdeBaseDatos = true;
        }

        private static IReadOnlyDictionary<int, string>? Normalizar(IEnumerable<(int Codigo, string Descripcion)> tipos)
        {
            if (tipos is null) return null;

            var nuevo = tipos
                .Where(t => t.Codigo > 0 && !string.IsNullOrWhiteSpace(t.Descripcion))
                .GroupBy(t => t.Codigo)
                .ToDictionary(g => g.Key, g => g.First().Descripcion.Trim());

            return nuevo.Count == 0 ? null : nuevo;
        }
    }
}