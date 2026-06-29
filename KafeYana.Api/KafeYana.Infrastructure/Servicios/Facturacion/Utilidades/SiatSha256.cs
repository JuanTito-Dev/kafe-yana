using System;
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

        public static string GenerarHashArchivo(string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo es requerido para calcular el hash.", nameof(archivo));

            // FIX GAP 11.f: SIAT hashea los BYTES del archivo (gzip), no el string
            // base64. Hashear Encoding.UTF8.GetBytes(archivo) — como estaba antes —
            // producía un hash sobre los caracteres ASCII del base64 (incluyendo \r\n),
            // que nunca coincidía con el hash que SIAT calculaba sobre los bytes gzip
            // del archivo decodificado. Resultado: rechazo con 1002/1003 aún cuando el
            // formato del archivo ya era válido. Ver [[kafeyana-siat-gap-920]].
            var bytesArchivo = Convert.FromBase64String(archivo);
            return Generar(bytesArchivo);
        }
    }
}
