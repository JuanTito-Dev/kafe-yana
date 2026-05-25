namespace KafeYana.Application.Dtos.PromocionTemporadaDtos
{
    public sealed class DtoPromocionTemporadaProductoItem
    {
        public int IdPromocionTemporada { get; init; }

        public required string NombrePromocion { get; init; }

        public DateTime FechaInicio { get; init; }

        public DateTime FechaFin { get; init; }

        public int IdProductoCanjeable { get; init; }

        public required string NombreProducto { get; init; }

        public required string Categoria { get; init; }

        public int Puntos { get; init; }
    }

    public sealed class DtoPromocionesTemporadaCliente
    {
        public int Id_Cliente { get; init; }

        public IReadOnlyList<DtoPromocionTemporadaProductoItem> Productos { get; init; } = [];
    }

    public sealed class DtoReclamarPromocionTemporada
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int IdCliente { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int IdPromocionTemporada { get; set; }
    }

    public sealed class ResultadoReclamoPromocionTemporada
    {
        public required string Mensaje { get; init; }

        public int IdPromocionTemporada { get; init; }

        public required string NombrePromocion { get; init; }

        public required string CodigoReclamo { get; init; }

        public IReadOnlyList<DtoPromocionTemporadaProductoReclamado> ProductosReclamados { get; init; } = [];
    }

    public sealed class DtoPromocionTemporadaProductoReclamado
    {
        public int IdProductoCanjeable { get; init; }

        public required string NombreProducto { get; init; }

        public required string Categoria { get; init; }
    }
}
