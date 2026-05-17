using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Registro de cada transacción de puntos asociada a una venta.
    /// </summary>
    public class HistorialPuntos : BaseEntity
    {
        public int Id_Cliente { get; set; }

        public Cliente? Cliente { get; set; }

        public required string CodigoVenta { get; set; }

        public int PuntosBase { get; set; }

        public int PuntosFinales { get; set; }

        /// <summary>Texto con el detalle de qué aceleradores se aplicaron. Ej: "CompraAlta:x2=22 | Cumpleanos:x3=33 | HoraValle:+4"</summary>
        public string? Desglose { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
