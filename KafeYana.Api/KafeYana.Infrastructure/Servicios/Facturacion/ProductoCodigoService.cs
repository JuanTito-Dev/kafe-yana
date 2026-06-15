using System.Globalization;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class ProductoCodigoService
    {
        public static string Generar(int id) =>
            id.ToString("D5", CultureInfo.InvariantCulture);
    }
}
