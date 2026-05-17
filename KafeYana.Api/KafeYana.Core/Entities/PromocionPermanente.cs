using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    public class PromocionPermanente : BaseEntity
    {
        public required string Nombre { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        /// <summary>NCompras | MontoMinimo | Requeridos</summary>
        public required string TipoCondicion { get; set; }

        public int ValorCondicion { get; set; }

        /// <summary>PuntosExtra | ProductoGratis | Descuento</summary>
        public required string TipoRecompensa { get; set; }

        /// <summary>Puntos extra, porcentaje de descuento o N/A si es producto gratis.</summary>
        public int ValorRecompensa { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Solo obligatorio cuando TipoRecompensa = ProductoGratis.</summary>
        public int? Id_ProductoCanjeable { get; set; }

        public ProductoCanjeable? ProductoCanjeable { get; set; }
    }
}
