using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace KafeYana.Infrastructure.Servicios
{
    public class ProductoImagenService : IProductoImagenService
    {
        private static readonly string[] _formatosPermitidos = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] _mimePermitidos = ["image/jpeg", "image/png", "image/webp"];

        private readonly IR2StorageService _r2;
        private readonly ICategoriaRepositorio _categorias;
        private readonly CloudflareR2Options _opts;

        public ProductoImagenService(
            IR2StorageService r2,
            ICategoriaRepositorio categorias,
            IOptions<CloudflareR2Options> opts)
        {
            _r2 = r2;
            _categorias = categorias;
            _opts = opts.Value;
        }

        public async Task<string> ProcesarSubidaAsync(IFormFile imagen, string nombreProducto, int categoriaId)
        {
            ValidarFormato(imagen);

            var categoria = await _categorias.FindByIdAsync(categoriaId);
            var carpeta = categoria is not null
                ? ToSlug(categoria.Nombre)
                : "general";

            var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            var nombreArchivo = $"{GenerarPrefijo()}-{ToSlug(nombreProducto)}{extension}";
            var key = $"productos/{carpeta}/{nombreArchivo}";

            using var stream = imagen.OpenReadStream();
            return await _r2.SubirAsync(stream, imagen.ContentType, key);
        }

        public async Task EliminarSiExisteAsync(string urlImagen)
        {
            if (string.IsNullOrWhiteSpace(urlImagen)) return;

            var baseUrl = _opts.PublicUrl.TrimEnd('/');
            if (!urlImagen.StartsWith(baseUrl)) return;

            var key = urlImagen[(baseUrl.Length + 1)..];
            await _r2.EliminarAsync(key);
        }

        private static void ValidarFormato(IFormFile imagen)
        {
            if (imagen.Length == 0)
                throw new ImagenException("La imagen enviada está vacía.");

            var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

            if (!_formatosPermitidos.Contains(extension))
                throw new ImagenException($"Formato no permitido. Solo se aceptan: {string.Join(", ", _formatosPermitidos)}.");

            if (!_mimePermitidos.Contains(imagen.ContentType.ToLowerInvariant()))
                throw new ImagenException("El tipo de contenido de la imagen no es válido.");
        }

        private static string GenerarPrefijo()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        private static string ToSlug(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "sin-nombre";

            texto = texto.ToLowerInvariant().Trim();

            var reemplazos = new Dictionary<char, char>
            {
                ['á'] = 'a', ['é'] = 'e', ['í'] = 'i', ['ó'] = 'o', ['ú'] = 'u',
                ['ä'] = 'a', ['ë'] = 'e', ['ï'] = 'i', ['ö'] = 'o', ['ü'] = 'u',
                ['ñ'] = 'n', ['ç'] = 'c'
            };

            var sb = new StringBuilder();
            foreach (var c in texto)
            {
                if (reemplazos.TryGetValue(c, out var reemplazo))
                    sb.Append(reemplazo);
                else if (char.IsLetterOrDigit(c))
                    sb.Append(c);
                else if (c == ' ' || c == '-')
                    sb.Append('-');
            }

            return Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        }
    }
}
