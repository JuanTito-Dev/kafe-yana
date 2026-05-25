namespace KafeYana.Application.Dtos.ProductoCanjeable
{
    public sealed class DtoPromocionGratisItem
    {
        public int IdPromocionPermanente { get; init; }

        public required string NombrePromocion { get; init; }

        public required string TipoCondicion { get; init; }

        public int ValorCondicion { get; init; }

        /// <summary>Solo aplica cuando TipoCondicion = NCompras.</summary>
        public int? ProgresoActual { get; init; }

        public int IdProductoCanjeable { get; init; }

        public required string NombreProducto { get; init; }

        public required string Categoria { get; init; }
    }

    public sealed class DtoPromocionesGratisCliente
    {
        public int Id_Cliente { get; init; }

        public IReadOnlyList<DtoPromocionGratisItem> Disponibles { get; init; } = [];

        public IReadOnlyList<DtoPromocionGratisItem> EnProgreso { get; init; } = [];
    }

    public sealed class ResultadoReclamoPromocionGratis
    {
        public required string Mensaje { get; init; }

        public int IdPromocionPermanente { get; init; }

        public required string NombrePromocion { get; init; }

        public int IdProductoCanjeable { get; init; }

        public required string NombreProducto { get; init; }

        public required string Categoria { get; init; }
    }
}
