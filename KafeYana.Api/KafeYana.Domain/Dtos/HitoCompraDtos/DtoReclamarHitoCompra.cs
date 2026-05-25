using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.HitoCompraDtos
{
    public class DtoReclamarHitoCompra
    {
        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdHitoCompra { get; set; }
    }

    public sealed class ResultadoReclamoHitoCompra
    {
        public required string Mensaje { get; init; }

        public int IdHitoCompra { get; init; }

        public int NumeroComprasRequerido { get; init; }

        public int NumeroComprasCliente { get; init; }

        public required string CodigoReclamo { get; init; }

        public int IdProductoCanjeable { get; init; }

        public required string NombreProducto { get; init; }

        public required string Categoria { get; init; }
    }
}
