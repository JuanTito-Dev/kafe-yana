using KafeYana.Domain.Entities.BaseEntidades;
using System.Collections.Generic;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Detalle_ronda : BaseEntity
    {
        public int Id_Ronda { get; set; }

        public required int Id_Producto { get; set; }

        public required string Nombre_Producto { get; set; }

        public required int Cantidad { get; set; }

        public decimal Precio { get; set; } = 0.00M;

        public Ronda? ronda { get; set; }
    
        public Producto? producto { get; set; }

        public List<Detalle_Ronda_Opcion> Opciones { get; set; } = new List<Detalle_Ronda_Opcion>();
    }
}
