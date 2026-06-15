using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class LeyendaSiatService
    {
        public static string ObtenerTexto(LeyendaSiat leyenda) =>
            LeyendaSiatTextos.Todas[(int)leyenda];

        public static string ObtenerAleatoria() =>
            LeyendaSiatTextos.Todas[Random.Shared.Next(LeyendaSiatTextos.Todas.Length)];
    }
}
