using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Promocion : BaseEntity
    {
        public int Producto_Id { get; set; }

        public Producto Producto { get; set; }

        public ICollection<PromocionDetalle> Detalles { get; set; } = new List<PromocionDetalle>();

        public ProductoMovimiento CrearMovimientoVenta(int cantidad, string codigo) =>
            Producto.Movimiento(cantidad, TipoMovimientos.Venta.ToString(), codigo, 0, Producto.Precio);

        public ProductoMovimiento Venta(int cantidad, string codigo)
        {
            return Producto.Movimiento(cantidad, TipoMovimientos.Venta.ToString(), codigo, 0, Producto.Precio);
        }

        public ProductoMovimiento Canje(int cantidad, string codigo)
        {
            return Producto.Movimiento(cantidad, TipoMovimientos.Canje.ToString(), codigo, 0, Producto.Precio);
        }
    }
}
