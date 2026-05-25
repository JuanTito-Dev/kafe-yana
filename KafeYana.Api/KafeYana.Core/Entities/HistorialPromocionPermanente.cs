using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Registro de cada aplicación de una promoción permanente en una venta.</summary>
    public class HistorialPromocionPermanente : BaseEntity
    {
        public int Id_Cliente { get; set; }

        public int Id_PromocionPermanente { get; set; }

        public required string CodigoVenta { get; set; }

        public required string TipoRecompensa { get; set; }

        public int ValorRecompensa { get; set; }

        public required string TipoCondicion { get; set; }

        public int ValorCondicion { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Cliente? Cliente { get; set; }

        public PromocionPermanente? PromocionPermanente { get; set; }
    }
}
