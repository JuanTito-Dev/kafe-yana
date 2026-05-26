using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Registro de reclamo de un hito por compras (una vez por cliente).</summary>
    public class HistorialHitoCompra : BaseEntity
    {
        public int Id_Cliente { get; set; }

        public int Id_HitoCompra { get; set; }

        public int NumeroComprasAlReclamar { get; set; }

        public required string CodigoReclamo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Cliente? Cliente { get; set; }

        public HitoCompra? HitoCompra { get; set; }
    }
}
