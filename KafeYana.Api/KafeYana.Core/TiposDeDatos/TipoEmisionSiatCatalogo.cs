using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>
    /// Catálogo de tipos de emisión leído desde la BD
    /// (tabla <c>CatTiposEmision</c>, sincronizada por
    /// <c>SincronizadorCatTipoEmision</c>).
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
    /// Espejo de <see cref="TipoDocumentoIdentidadSiatCatalogo"/> (mismo patrón:
    /// catálogo paramétrico del SIAT leído en cada request de venta).
    /// </summary>
    public static class TipoEmisionSiatCatalogo
    {
        // IMPORTANTE: declarar FallbackHardcoded ANTES de _cache.
        // C# ejecuta los field initializers estáticos en orden textual.
        // Si _cache se declara primero y referencia FallbackHardcoded,
        // cuando corre su initializer FallbackHardcoded todavía es null
        // (default de reference type) y _cache queda apuntando a null
        // hasta el primer Refrescar() exitoso. Si el sync falla al boot
        // (típico: SIAT caído), _cache permanece null para siempre y
        // cualquier validación de tipo de emisión NRE en EsValido. Ver
        // [[kafeyana-catalogo-typeinit-duplicate-keys]] para el bug
        // original (ToDictionary con duplicados); este es el primo hermano:
        // forward reference entre static fields.
        private static readonly IReadOnlyDictionary<int, string> FallbackHardcoded =
            new Dictionary<int, string>
            {
                [1] = "EN LINEA",
                [2] = "FUERA DE LINEA",
                [3] = "MASIVO",
                [4] = "CONTINGENCIA",
            };

        // Snapshot inmutable de los tipos conocidos. Se reemplaza atómicamente
        // vía Interlocked.Exchange para que los lectores vean una versión estable.
        private static volatile IReadOnlyDictionary<int, string> _cache = FallbackHardcoded;

        /// <summary>
        /// True mientras el caché contenga los valores de <see cref="FallbackHardcoded"/>
        /// (server arrancó pero ningún sync del SIAT corrió todavía). Pasa a false
        /// en cuanto <see cref="Refrescar"/> recibe tipos válidos del SIN.
        ///
        /// Útil para que el operador/administrador pueda distinguir
        /// "catálogo real del SIN" de "fallback porque el sync nunca corrió".
        /// </summary>
        public static bool EsFallback { get; private set; } = true;

        /// <summary>True si el código está en el catálogo vigente (BD o fallback).</summary>
        public static bool EsValido(int codigo) => _cache.ContainsKey(codigo);

        /// <summary>
        /// Descripción del tipo de emisión. Si no existe en el catálogo, devuelve
        /// "Tipo {codigo}" en lugar de lanzar para no romper respuestas al cliente.
        /// </summary>
        public static string ObtenerDescripcion(int codigo) =>
            _cache.TryGetValue(codigo, out var d) ? d : $"Tipo {codigo}";

        /// <summary>
        /// Snapshot de solo-lectura del catálogo actual. Útil para la UI
        /// (cuando se implemente el selector de modo de emisión).
        /// </summary>
        public static IReadOnlyDictionary<int, string> ObtenerTodos() => _cache;

        /// <summary>
        /// Llamado por <c>SincronizadorCatTipoEmision</c> al terminar una
        /// sync exitosa. Reemplaza el caché atómicamente con los tipos del SIN.
        /// </summary>
        public static void Refrescar(IEnumerable<(int Codigo, string Descripcion)> tipos)
        {
            if (tipos is null) return;

            var nuevo = tipos
                .Where(t => t.Codigo > 0 && !string.IsNullOrWhiteSpace(t.Descripcion))
                .GroupBy(t => t.Codigo)
                .ToDictionary(g => g.Key, g => g.First().Descripcion.Trim());

            if (nuevo.Count == 0) return;

            // Reemplazo atómico: cualquier lector en vuelo verá el diccionario viejo
            // o el nuevo, nunca uno parcial.
            Interlocked.Exchange(ref _cache, nuevo);
            EsFallback = false;
        }
    }
}
