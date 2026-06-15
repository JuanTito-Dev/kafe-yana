using HotChocolate;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Elaborado : BaseEntity
    {
        public required string Unidad_medida { get; set; }

        public int CodigoUnidadMedida { get; set; }

        public required bool Producible { get; set; }

        public int Stock_actual { get; private set; } = 0;
        
        public int Id_Producto { get; set; }

        public string Ubicacion { get; set; } = string.Empty;

        //Navegacion
        public Producto Producto { get; set; }

        public Receta? Receta { get; set; }

        public List<Variacion>? Variaciones { get; set; } = new List<Variacion>();

        [GraphQLIgnore]
        public ProductoMovimiento AjusteEntrada(int Cantidad, int Porciones, Stock_Ajuste ajuste, decimal costo)
        {
            Stock_actual += Cantidad * Porciones;
            ajuste.StockNuevo = Stock_actual;
            ajuste.Ajuste = ajuste.StockNuevo - ajuste.StockAnterior;
            ajuste.Perdida = 0;
            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";

            var movimiento = this.Producto.Movimiento(Cantidad * Porciones, TipoMovimientos.Ajuste.ToString(), Referencia, Stock_actual, costo / Porciones);

            return movimiento;
        }

        [GraphQLIgnore]
        public ProductoMovimiento AjusteSalida(int cantidad, decimal costo, Stock_Ajuste ajuste)
        {
            Stock_actual -= cantidad;
            ajuste.StockNuevo = Stock_actual;
            ajuste.Ajuste = ajuste.StockNuevo - ajuste .StockAnterior;

            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";

            var movimiento = this.Producto.Movimiento(-cantidad, TipoMovimientos.Ajuste.ToString(), Referencia, Stock_actual, costo);

            return movimiento;
        }

        [GraphQLIgnore]
        public void ComprometerStock(int cantidad)
        {
            if (Producible)
                Stock_actual -= cantidad;
        }

        [GraphQLIgnore]
        public void DevolverStock(int cantidad)
        {
            if (Producible)
                Stock_actual += cantidad;
        }

        [GraphQLIgnore]
        public ProductoMovimiento CrearMovimientoVenta(int cantidad, string codigo, decimal costo) =>
            Producto.Movimiento(-cantidad, TipoMovimientos.Venta.ToString(), codigo, Stock_actual, costo);

        [GraphQLIgnore] 
        public ProductoMovimiento Venta(int cantidad, string Codigo, decimal costo)
        {
            if (Producible)
            {
                Stock_actual -= cantidad;
            }

            var movimiento = this.Producto.Movimiento(-cantidad, TipoMovimientos.Venta.ToString(), Codigo, Stock_actual, costo);

            return movimiento;
        }

        [GraphQLIgnore]
        public ProductoMovimiento Canje(int cantidad, string Codigo, decimal costo)
        {
            if (Producible)
                Stock_actual -= cantidad;

            return Producto.Movimiento(-cantidad, TipoMovimientos.Canje.ToString(), Codigo, Stock_actual, costo);
        }
    }
}
