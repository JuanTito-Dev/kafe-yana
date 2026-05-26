namespace KafeYana.Application.Dtos.VentaDtos
{
    public sealed class ResultadoAplicacionPromocionPermanente
    {
        public required string NombrePromocion { get; init; }

        public int PuntosExtra { get; init; }

        public required string Mensaje { get; init; }
    }
}
