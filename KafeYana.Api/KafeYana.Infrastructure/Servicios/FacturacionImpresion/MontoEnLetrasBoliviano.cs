using System.Globalization;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    internal static class MontoEnLetrasBoliviano
    {
        private static readonly string[] Unidades =
        [
            "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
            "diez", "once", "doce", "trece", "catorce", "quince", "dieciseis", "diecisiete", "dieciocho", "diecinueve"
        ];

        private static readonly string[] Decenas =
        [
            "", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"
        ];

        private static readonly string[] Centenas =
        [
            "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
            "seiscientos", "setecientos", "ochocientos", "novecientos"
        ];

        public static string Formatear(decimal monto)
        {
            var entero = (long)Math.Floor(monto);
            var centavos = (int)Math.Round((monto - entero) * 100, 0, MidpointRounding.AwayFromZero);
            if (centavos == 100)
            {
                entero++;
                centavos = 0;
            }

            var letras = Convertir(entero);
            if (entero == 1)
                letras = "un";
            if (entero == 0)
                letras = "cero";

            return $"{Capitalizar(letras)} {centavos:D2}/100 Bolivianos";
        }

        private static string Convertir(long numero)
        {
            if (numero == 0) return "cero";
            if (numero < 0) return "menos " + Convertir(-numero);
            if (numero < 20) return Unidades[numero];
            if (numero < 100)
            {
                var d = (int)(numero / 10);
                var u = (int)(numero % 10);
                if (numero is > 20 and < 30)
                    return u == 0 ? "veinte" : $"veinti{Unidades[u]}";
                return u == 0 ? Decenas[d] : $"{Decenas[d]} y {Unidades[u]}";
            }
            if (numero == 100) return "cien";
            if (numero < 1000)
            {
                var c = (int)(numero / 100);
                var r = numero % 100;
                return r == 0 ? Centenas[c] : $"{Centenas[c]} {Convertir(r)}";
            }
            if (numero < 1_000_000)
            {
                var miles = numero / 1000;
                var r = numero % 1000;
                var pref = miles == 1 ? "mil" : $"{Convertir(miles)} mil";
                return r == 0 ? pref : $"{pref} {Convertir(r)}";
            }

            var millones = numero / 1_000_000;
            var resto = numero % 1_000_000;
            var prefijo = millones == 1 ? "un millon" : $"{Convertir(millones)} millones";
            return resto == 0 ? prefijo : $"{prefijo} {Convertir(resto)}";
        }

        private static string Capitalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var palabra in palabras)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(char.ToUpper(palabra[0], CultureInfo.InvariantCulture));
                if (palabra.Length > 1)
                    sb.Append(palabra[1..]);
            }
            return sb.ToString();
        }
    }
}
