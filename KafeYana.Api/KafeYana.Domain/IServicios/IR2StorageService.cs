namespace KafeYana.Application.IServicios
{
    public interface IR2StorageService
    {
        /// <summary>
        /// Sube un archivo a R2 y devuelve la URL pública.
        /// </summary>
        /// <param name="stream">Contenido del archivo</param>
        /// <param name="contentType">MIME type (ej: image/jpeg)</param>
        /// <param name="key">Ruta completa dentro del bucket (ej: productos/bebidas/abc123-coca-cola.jpg)</param>
        Task<string> SubirAsync(Stream stream, string contentType, string key);

        /// <summary>
        /// Elimina un archivo de R2 por su clave (key).
        /// </summary>
        Task EliminarAsync(string key);
    }
}
