using System.Globalization;
using System.Numerics;

namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>Utilidades SIAT: ceros a la izquierda y Base16 (CUF).</summary>
    public static class SiatStringTools
    {
        public static string CompletarCeros(string valor, int longitudMaxima, bool rellenarDerecha = false)
        {
            if (valor.Length >= longitudMaxima)
                return valor;

            var faltantes = longitudMaxima - valor.Length;
            return rellenarDerecha
                ? valor + new string('0', faltantes)
                : new string('0', faltantes) + valor;
        }

        public static string Base16(string cadenaDecimal)
        {
            var valor = BigInteger.Parse(cadenaDecimal, CultureInfo.InvariantCulture);
            return valor.ToString("X", CultureInfo.InvariantCulture);
        }

        public static string Base10(string cadenaHexadecimal)
        {
            var valor = BigInteger.Parse(cadenaHexadecimal, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return valor.ToString(CultureInfo.InvariantCulture);
        }
    }
}
