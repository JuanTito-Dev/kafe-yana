using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class OrdenItemProducto : BaseEntity
    {
        public int Id_Orden { get; set; }
        public int? Id_Producto { get; set; }
        public required string Nombre { get; set; }
        public required int Cantidad { get; set; }
        public required decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public OrdenCompra? Orden { get; set; }
        public Producto? Producto { get; set; }
    }
}
