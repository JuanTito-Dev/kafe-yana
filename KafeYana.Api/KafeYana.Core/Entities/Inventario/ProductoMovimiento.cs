using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class ProductoMovimiento : BaseMovimientos
    {
        public int Id_Producto { get; private set; }

        public Producto? Producto {  get; set; }

        public ProductoMovimiento() { }

        public ProductoMovimiento(int Id_Producto, string Tipo, string referencia, int cantidad, decimal costo, int stock) : 
            base(Tipo, referencia, cantidad, costo, stock)
        {
            this.Id_Producto = Id_Producto;
        }
    }
}
