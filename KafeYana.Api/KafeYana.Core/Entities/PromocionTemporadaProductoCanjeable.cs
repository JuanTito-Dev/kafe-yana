namespace KafeYana.Domain.Entities
{
    /// <summary>Tabla de unión: una temporada puede incluir varios productos canjeables.</summary>
    public class PromocionTemporadaProductoCanjeable
    {
        public int Id_PromocionTemporada { get; set; }

        public PromocionTemporada PromocionTemporada { get; set; } = null!;

        public int Id_ProductoCanjeable { get; set; }

        public ProductoCanjeable ProductoCanjeable { get; set; } = null!;
    }
}
