namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>Ayudas para campos opcionales del XML SIAT (xsi:nil).</summary>
    public static class SiatXmlHelper
    {
        public static bool EsNulo(string? valor) => string.IsNullOrWhiteSpace(valor);
    }
}
