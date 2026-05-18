using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Hito por número de compras: al alcanzar N compras se asocia una recompensa (producto canjeable).</summary>
    public class HitoCompra : BaseEntity
    {
        public int NumeroCompras { get; set; }

        public int Id_ProductoCanjeable { get; set; }

        public ProductoCanjeable? ProductoCanjeable { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public string Icono { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
