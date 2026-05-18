using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    public class PromocionTemporada : BaseEntity
    {
        public required string Nombre { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<PromocionTemporadaProductoCanjeable> ProductosCanjeables { get; set; }
            = new List<PromocionTemporadaProductoCanjeable>();
    }
}
