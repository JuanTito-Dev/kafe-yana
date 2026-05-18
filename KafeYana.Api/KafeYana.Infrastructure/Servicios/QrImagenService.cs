using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios;
using Microsoft.AspNetCore.Http;

namespace KafeYana.Infrastructure.Servicios
{
    public class QrImagenService(IR2StorageService r2) : IQrImagenService
    {
        private static readonly string[] FormatosPermitidos = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] MimePermitidos = ["image/jpeg", "image/png", "image/webp"];

        public async Task<string> SubirAsync(IFormFile imagen)
        {
            ValidarFormato(imagen);

            var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            var prefijo = GenerarPrefijo();
            var key = $"Qr/{prefijo}-qr{extension}";

            using var stream = imagen.OpenReadStream();
            return await r2.SubirAsync(stream, imagen.ContentType, key);
        }

        private static void ValidarFormato(IFormFile imagen)
        {
            if (imagen.Length == 0)
                throw new ImagenException("La imagen enviada está vacía.");

            var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

            if (!FormatosPermitidos.Contains(extension))
                throw new ImagenException($"Formato no permitido. Solo se aceptan: {string.Join(", ", FormatosPermitidos)}.");

            if (!MimePermitidos.Contains(imagen.ContentType.ToLowerInvariant()))
                throw new ImagenException("El tipo de contenido de la imagen no es válido.");
        }

        private static string GenerarPrefijo()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
