using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Fila única con puntos que se otorgan al referidor y al referido cuando el programa está activo.</summary>
    public class ReferidosConfig : BaseEntity
    {
        public int PuntosReferidor { get; set; }

        public int PuntosReferido { get; set; }

        public bool Activo { get; set; }
    }
}
