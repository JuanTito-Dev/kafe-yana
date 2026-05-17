using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Acelerador de puntos. Cada fila representa un tipo de acelerador.
    /// Tipos: Combo, CompraAlta, CompraMediana, Cumpleanos, HoraValle.
    /// TipoAplicacion: Suma (agrega al resultado final) | Multiplicador (multiplica la base individualmente).
    /// </summary>
    public class AceleradorPuntos : BaseEntity
    {
        public required string Tipo { get; set; }

        public required string TipoAplicacion { get; set; }

        public decimal Cantidad { get; set; }

        /// <summary>Solo para CompraAlta (100) y CompraMediana (70). Monto mínimo de compra para activarse.</summary>
        public decimal? UmbralMonto { get; set; }

        /// <summary>Solo para HoraValle. Hora de inicio del rango.</summary>
        public TimeOnly? HoraInicio { get; set; }

        /// <summary>Solo para HoraValle. Hora de fin del rango.</summary>
        public TimeOnly? HoraFin { get; set; }

        public bool Activo { get; set; } = false;
    }
}
