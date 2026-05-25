using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Registro de reclamo de promoción por temporada (una vez por cliente).</summary>
    public class HistorialPromocionTemporada : BaseEntity
    {
        public int Id_Cliente { get; set; }

        public int Id_PromocionTemporada { get; set; }

        public required string CodigoReclamo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Cliente? Cliente { get; set; }

        public PromocionTemporada? PromocionTemporada { get; set; }
    }
}
