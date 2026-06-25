using System;

namespace KafeYana.Application.Exceptions
{
    /// <summary>
    /// Se lanza cuando el sistema intenta usar un catálogo del SIAT
    /// que aún no ha sido sincronizado (tabla vacía).
    /// Obliga a ejecutar la sincronización antes de continuar.
    /// </summary>
    public class CatalogoNoSincronizadoException : Exception
    {
        public string Catalogo { get; }

        public CatalogoNoSincronizadoException(string catalogo)
            : base($"El catálogo '{catalogo}' aún no ha sido sincronizado con el SIAT. "
                 + "Ejecute POST /api/catalogos/sincronizar-{catalogo} o reinicie el servidor.")
        {
            Catalogo = catalogo;
        }
    }
}