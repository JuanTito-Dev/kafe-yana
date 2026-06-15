namespace KafeYana.Domain.TiposDeDatos
{
    public record UnidadMedidaSiatItem(int Codigo, string Descripcion);

    public static class UnidadMedidaSiatCatalogo
    {
        public static readonly IReadOnlyDictionary<string, int> PorDescripcion = new Dictionary<string, int>
        {
            ["UNIDAD"] = 57,
            ["VASO"] = 97,
            ["BOTELLA"] = 5,
            ["CAJA"] = 6,
            ["MILIGRAMO"] = 33,
            ["GRAMO"] = 17,
            ["LITRO"] = 28,
            ["MILILITRO"] = 34,
            ["TAZA"] = 57,
            ["PORCION"] = 57,
            ["PLATO"] = 57,
            ["OTRO"] = 62
        };
    }
}
