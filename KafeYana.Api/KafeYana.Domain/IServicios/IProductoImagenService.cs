using Microsoft.AspNetCore.Http;

namespace KafeYana.Application.IServicios
{
    public interface IProductoImagenService
    {
        /// <summary>
        /// Valida el formato, genera nombre único, organiza por categoría y sube la imagen a R2.
        /// Devuelve la URL pública de la imagen.
        /// </summary>
        Task<string> ProcesarSubidaAsync(IFormFile imagen, string nombreProducto, int categoriaId);

        /// <summary>
        /// Elimina la imagen de R2 si la URL no está vacía.
        /// </summary>
        Task EliminarSiExisteAsync(string urlImagen);
    }
}
