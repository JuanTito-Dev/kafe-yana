using System.IO.Compression;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>
    /// Compresión GZIP del XML de factura SIAT.
    /// El envío usa Base64 (campo archivo), no Base16 — Base16 es solo para el CUF.
    /// </summary>
    public static class SiatGzip
    {
        public static byte[] Comprimir(byte[] datos)
        {
            using var salida = new MemoryStream();
            using (var gzip = new GZipStream(salida, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(datos, 0, datos.Length);
            }

            return salida.ToArray();
        }

        public static byte[] ComprimirXml(string xml) =>
            Comprimir(Encoding.UTF8.GetBytes(xml));

        public static string ComprimirABase64(byte[] datosComprimidos) =>
            Convert.ToBase64String(datosComprimidos);

        public static string ComprimirXmlABase64(string xml) =>
            ComprimirABase64(ComprimirXml(xml));

        /// <summary>Decodifica Base64 + descomprime GZIP para diagnóstico/log.</summary>
        public static string DescomprimirBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            using var entrada = new MemoryStream(bytes);
            using var gzip = new GZipStream(entrada, CompressionMode.Decompress);
            using var lector = new StreamReader(gzip, Encoding.UTF8);
            return lector.ReadToEnd();
        }
    }
}
