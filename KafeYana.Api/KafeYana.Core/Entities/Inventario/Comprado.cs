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
    public class Comprado : BaseEntity 
    {
        public string Codigo_barra { get; set; } = string.Empty;

        public required string Unidad_medida { get; set; }

        public int CodigoUnidadMedida { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public required decimal Costo_compra { get; set; } 

        public int Stock_actual { get; private set; }

        public required int Stock_minimo { get; set; }

        public required bool Disponible { get; set; }

        //FK
        public int Id_Producto { get; set; }
        
        //Navegacion
        public Producto? Producto { get; set; }

        public void DefinirStock(int Stock)
        {
            Stock_actual = Stock;
        }

        public Comprado() { }

        public Comprado(int Stock)
        {
            Stock_actual = Stock;
        }

        public void EditarStock(int Cantidad)
        {
            Stock_actual = Cantidad;
        }

        [GraphQLIgnore]
        public (Stock_Ajuste, ProductoMovimiento) AjusteEntrada(string Usuario, int Cantidad, string Nota, string motivo)
        {
            var stockAnterior = Stock_actual;
            Stock_actual += Cantidad;
            var stockNuevo = Stock_actual;
            var ajuste = new Stock_Ajuste
            {
                Nombre = Producto!.Nombre,
                Tipo = Producto.Tipo,
                Usuario = Usuario,
                Perdida = 0,
                StockAnterior = stockAnterior,
                StockNuevo = stockNuevo,
                Ajuste = stockNuevo - stockAnterior,
                Nota = Nota,
                Motivo = motivo,
                Fecha = DateTime.UtcNow
            };

            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";

            var Moviento = this.Producto.Movimiento(Cantidad, TipoMovimientos.Ajuste.ToString(), Referencia, Stock_actual, Costo_compra);

            return (ajuste, Moviento);
        }

        [GraphQLIgnore]
        public (Stock_Ajuste, ProductoMovimiento) AjusteSalida(string Usuario, int Cantidad, string Nota, string motivo)
        {
            var stockAnterior = Stock_actual;
            Stock_actual -= Cantidad;
            var stockNuevo = Stock_actual;

            var ajuste = new Stock_Ajuste
            {
                Nombre = Producto!.Nombre,
                Tipo = Producto.Tipo,
                Usuario = Usuario,
                Perdida = Costo_compra * Cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = stockNuevo,
                Ajuste = stockNuevo - stockAnterior, // será negativo automáticamente
                Nota = Nota,
                Motivo = motivo,
                Fecha = DateTime.UtcNow
            };

            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";
            var Movimiento = Producto.Movimiento(-Cantidad, TipoMovimientos.Ajuste.ToString(), Referencia, Stock_actual, Costo_compra);

            return (ajuste, Movimiento);
        }

        [GraphQLIgnore]
        public void ComprometerStock(int cantidad) => Stock_actual -= cantidad;

        [GraphQLIgnore]
        public void DevolverStock(int cantidad) => Stock_actual += cantidad;

        [GraphQLIgnore]
        public ProductoMovimiento CrearMovimientoVenta(int cantidad, string codigo) =>
            Producto!.Movimiento(-cantidad, TipoMovimientos.Venta.ToString(), codigo, Stock_actual, Costo_compra);

        [GraphQLIgnore]
        public ProductoMovimiento Venta(int Cantidad, string Codigo)
        {
            Stock_actual -= Cantidad;

            var Movimiento = Producto!.Movimiento(-Cantidad, TipoMovimientos.Venta.ToString(), Codigo, Stock_actual, Costo_compra);

            return Movimiento;
        }

        [GraphQLIgnore]
        public ProductoMovimiento Canje(int Cantidad, string Codigo)
        {
            Stock_actual -= Cantidad;
            return Producto!.Movimiento(-Cantidad, TipoMovimientos.Canje.ToString(), Codigo, Stock_actual, Costo_compra);
        }

        [GraphQLIgnore]
        public ProductoMovimiento Compra(string referencia, int cantidad, decimal precioUnitario)
        {
            Stock_actual += cantidad;
            Costo_compra = precioUnitario;
            var movimiento = Producto!.Movimiento(cantidad, TipoMovimientos.Compra.ToString(), referencia, Stock_actual, precioUnitario);
            return movimiento;
        }
    }
}
