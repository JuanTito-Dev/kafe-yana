using System.Security.Cryptography;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>SHA-256 para hashArchivo SIAT (hex minúsculas).</summary>
    public static class SiatSha256
    {
        public static string Generar(byte[] datos)
        {
            var hash = SHA256.HashData(datos);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string GenerarHashArchivo(string archivo) =>
            Generar(Encoding.UTF8.GetBytes(archivo));
    }
}
