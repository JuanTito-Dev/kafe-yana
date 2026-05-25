using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Progreso del cliente en una promoción permanente (contador NCompras, etc.).
    /// Una fila por par (Cliente, Promoción).
    /// </summary>
    public class PromocionPermanenteProgreso : BaseEntity
    {
        public int Id_Cliente { get; set; }

        public int Id_PromocionPermanente { get; set; }

        /// <summary>Compras acumuladas desde la última aplicación/reclamo (NCompras).</summary>
        public int ContadorCompras { get; set; }

        /// <summary>Beneficio MontoMinimo pendiente de reclamar (ProductoGratis).</summary>
        public bool ReclamoMontoMinimoPendiente { get; set; }

        public Cliente? Cliente { get; set; }

        public PromocionPermanente? PromocionPermanente { get; set; }
    }
}
