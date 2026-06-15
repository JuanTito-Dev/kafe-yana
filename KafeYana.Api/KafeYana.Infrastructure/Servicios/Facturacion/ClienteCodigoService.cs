using System.Globalization;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class ClienteCodigoService
    {
        public static string Generar(string nombre, int id)
        {
            var inicial = ObtenerInicial(nombre);
            return $"{inicial}-{id:D4}";
        }

        private static char ObtenerInicial(string nombre)
        {
            var limpio = nombre.Trim();
            if (limpio.Length == 0)
                return 'X';

            return char.ToUpper(limpio[0], CultureInfo.InvariantCulture);
        }
    }
}
