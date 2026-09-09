namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    /// <summary>
    /// Emite un bitmap monocromo por el comando raster ESC/POS <c>GS v 0</c>.
    /// Lo soporta prácticamente cualquier térmica (es el mecanismo con el que se
    /// imprimen logos), a diferencia de comandos por hardware como el QR nativo
    /// <c>GS ( k</c> que varias impresoras/agentes ignoran en silencio.
    /// </summary>
    internal static class FacturaEscPosRaster
    {
        /// <param name="bits">
        /// Bitmap empaquetado, filas contiguas, MSB primero, bit en 1 = punto negro.
        /// Debe medir al menos <c>((anchoPx + 7) / 8) * altoPx</c> bytes.
        /// </param>
        public static void Escribir(MemoryStream ms, byte[] bits, int anchoPx, int altoPx)
        {
            var bytesPorFila = (anchoPx + 7) / 8;

            ms.WriteByte(0x1D);
            ms.WriteByte(0x76);
            ms.WriteByte(0x30);
            ms.WriteByte(0x00);
            ms.WriteByte((byte)(bytesPorFila % 256));
            ms.WriteByte((byte)(bytesPorFila / 256));
            ms.WriteByte((byte)(altoPx % 256));
            ms.WriteByte((byte)(altoPx / 256));
            ms.Write(bits, 0, bytesPorFila * altoPx);
            ms.WriteByte(0x0A);
        }
    }
}
