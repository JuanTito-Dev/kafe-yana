using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities.Inventario
{
    /// <summary>
    /// Desglose de un combo en una línea de ronda: snapshot por componente (nombre, cantidad vendida, ubicación).
    /// </summary>
    public class Detalle_Ronda_ComboItem : BaseEntity
    {
        public int Id_Detalle_Ronda { get; set; }

        public int Id_Producto { get; set; }

        public required string Nombre { get; set; }

        public int Cantidad { get; set; }

        public string Ubicacion { get; set; } = string.Empty;

        public Detalle_ronda? Detalle_Ronda { get; set; }

        public Producto? Producto { get; set; }
    }
}
