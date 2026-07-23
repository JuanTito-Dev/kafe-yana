using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Fachada de solo-lectura sobre <see cref="TipoDocumentoIdentidadSiatCatalogo"/>.
    ///
    /// Antes (pre-sync) leía de un enum hardcoded + diccionario en memoria.
    /// Ahora delega en el catálogo sincronizado contra el SIAT, que mantiene
    /// un caché thread-safe con reemplazo atómico.
    ///
    /// Mantener esta clase como wrapper permite:
    ///   1) Que los consumidores no tengan que cambiar namespace.
    ///   2) Migrar gradualmente cualquier caller que use los métodos directamente
    ///      hacia el catálogo si hace falta más adelante (logging, telemetría, etc.).
    /// </summary>
    public static class TipoDocumentoIdentidadSiatService
    {
        /// <summary>
        /// True si el código está en el catálogo vigente (BD sincronizada o fallback).
        /// Antes de cualquier sync: solo los 5 valores hardcoded (1..5).
        /// Después del primer sync: cualquier código que el SIN publique (1..N).
        /// </summary>
        public static bool EsValido(int codigo) =>
            TipoDocumentoIdentidadSiatCatalogo.EsValido(codigo);

        /// <summary>
        /// Descripción oficial del tipo de documento. Si el código no existe en el
        /// catálogo, devuelve "Tipo {codigo}" en lugar de lanzar para no romper
        /// respuestas al cliente.
        /// </summary>
        public static string ObtenerDescripcion(int codigo) =>
            TipoDocumentoIdentidadSiatCatalogo.ObtenerDescripcion(codigo);
    }
}