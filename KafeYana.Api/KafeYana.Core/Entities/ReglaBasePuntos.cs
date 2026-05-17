using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Regla base de acumulación: cada <see cref="Cantidad"/> bolivianos gastados = 1 punto.
    /// Solo debe existir una fila en la base de datos.
    /// </summary>
    public class ReglaBasePuntos : BaseEntity
    {
        public decimal Cantidad { get; set; }

        public bool Activo { get; set; } = true;
    }
}
