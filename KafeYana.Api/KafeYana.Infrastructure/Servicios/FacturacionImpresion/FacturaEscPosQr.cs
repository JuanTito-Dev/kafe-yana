using QRCoder;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    /// <summary>
    /// Renderiza el QR SIAT como bitmap monocromo y lo envía por el comando
    /// raster ESC/POS <c>GS v 0</c> (ver <see cref="FacturaEscPosRaster"/>).
    ///
    /// Antes se emitía la secuencia nativa <c>GS ( k</c> (QR por hardware), pero
    /// varias térmicas económicas y el agente de impresión intermedio no la
    /// implementan y la descartaban en silencio: el texto salía y el QR no.
    /// </summary>
    internal static class FacturaEscPosQr
    {
        /// <param name="maxAnchoPuntos">
        /// Ancho útil del cabezal en puntos (58 mm ≈ 384, 80 mm ≈ 576). Se usa
        /// para elegir la escala de cada módulo sin exceder el papel.
        /// </param>
        public static void Escribir(MemoryStream ms, string url, int maxAnchoPuntos = 384)
        {
            var matriz = GenerarMatriz(url);
            var modulos = matriz.GetLength(0);

            // Apuntar a ~200 px de lado (escanea bien y no ocupa medio ticket).
            var escala = Math.Clamp(200 / modulos, 3, 8);
            // Nunca exceder el ancho útil del cabezal.
            while (modulos * escala > maxAnchoPuntos && escala > 1) escala--;

            var anchoPx = modulos * escala;
            var bytesPorFila = (anchoPx + 7) / 8;
            var bits = new byte[bytesPorFila * anchoPx];

            for (var y = 0; y < anchoPx; y++)
            {
                var my = y / escala;
                for (var x = 0; x < anchoPx; x++)
                {
                    if (!matriz[my, x / escala]) continue;
                    bits[y * bytesPorFila + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }

            FacturaEscPosRaster.Escribir(ms, bits, anchoPx, anchoPx);
        }

        /// <summary>
        /// Matriz de módulos del QR (true = negro). Incluye la zona de silencio
        /// de 4 módulos que agrega QRCoder por defecto.
        /// </summary>
        private static bool[,] GenerarMatriz(string url)
        {
            using var generador = new QRCodeGenerator();
            using var datos = generador.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);

            var filas = datos.ModuleMatrix.Count;
            var matriz = new bool[filas, filas];
            for (var y = 0; y < filas; y++)
            {
                var fila = datos.ModuleMatrix[y];
                for (var x = 0; x < filas; x++)
                    matriz[y, x] = fila[x];
            }
            return matriz;
        }
    }
}
