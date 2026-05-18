using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Historial solo lectura / solo inserción desde la API.</summary>
    public class HistorialReferido : BaseEntity
    {
        public required string NombreReferidor { get; set; }

        public required string NombreReferido { get; set; }

        public int PuntosReferidor { get; set; }

        public int PuntosReferido { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
