using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class UnidadMedidaSiatService
    {
        private static readonly HashSet<int> CodigosValidos =
            UnidadMedidaSiatCatalogo.PorDescripcion.Values.ToHashSet();

        public static bool EsCodigoValido(int codigo) => CodigosValidos.Contains(codigo);

        public static bool TryResolver(string descripcion, out int codigo, out string descripcionCanonica)
        {
            if (UnidadMedidaSiatCatalogo.PorDescripcion.TryGetValue(descripcion, out codigo))
            {
                descripcionCanonica = descripcion;
                return true;
            }

            codigo = 0;
            descripcionCanonica = string.Empty;
            return false;
        }

        public static IReadOnlyList<UnidadMedidaSiatItem> Listar() =>
            UnidadMedidaSiatCatalogo.PorDescripcion
                .Select(x => new UnidadMedidaSiatItem(x.Value, x.Key))
                .ToList();
    }
}
