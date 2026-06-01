using HotChocolate;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Insumo : BaseEntity
    {
        public required string Nombre { get; set; }

        public required string Categoria { get; set; }

        public required string Unidad_min_uso { get; set; }

        public required string Unidad_compra {  get; set; }

        public required decimal Factor_conversion { get; set; }

        public required decimal Costo { get; set; }

        public int Stock_actual { get; private set; }

        public required int Stock_min { get; set; }

        public ICollection<Detalle> Detalles { get; set; }

        public ICollection<Ajuste> Ajustes { get; set; }

        public ICollection<Ajuste> AjustesComoNuevo { get; set; }

        public List<InsumoMovimiento> Movimientos { get; set; } = new List<InsumoMovimiento>();

        public List<OrdenItemInsumo> OrdenesInsumo { get; set; } = new List<OrdenItemInsumo>();
 
        private InsumoMovimiento RegistrarMovimiento(string Tipo, string referencia, int Cantidad, int Stock )
        {
            return new InsumoMovimiento(Id, Tipo, referencia, Cantidad, Costo/Factor_conversion, Stock);
        }

        [GraphQLIgnore]
        public (Stock_Ajuste, InsumoMovimiento) AjusteEntrada(string Usuario, int Cantidad, string Nota, string Motivo)
        {
            var stockanterior = Stock_actual;
            Stock_actual += Cantidad;
            var stockNuevo = Stock_actual;
            var ajuste = new Stock_Ajuste
            {
                Nombre = this.Nombre,
                Tipo = "Insumo",
                Usuario = Usuario,
                Perdida = 0,
                StockAnterior = stockanterior,
                StockNuevo = stockNuevo,
                Ajuste = stockNuevo - stockanterior,
                Nota = Nota,
                Motivo = Motivo,
                Fecha = DateTime.UtcNow
            };

            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";

            var Moviento = RegistrarMovimiento(TipoMovimientos.Ajuste.ToString(), Referencia, Cantidad, Stock_actual);

            return (ajuste, Moviento);
        }

        [GraphQLIgnore]
        public (Stock_Ajuste, InsumoMovimiento) AjusteSalida(string Usuario, int Cantidad, string Nota, string Motivo)
        {
            var stockanterior = Stock_actual;
            Stock_actual -= Cantidad;
            var stockNuevo = Stock_actual;
            var ajuste = new Stock_Ajuste
            {
                Nombre = this.Nombre,
                Tipo = "Insumo",
                Usuario = Usuario,
                Perdida = Costo * Cantidad,
                StockAnterior = stockanterior,
                StockNuevo = stockNuevo,
                Ajuste = stockNuevo - stockanterior,
                Nota = Nota,
                Motivo = Motivo,
                Fecha = DateTime.UtcNow
            };

            string Referencia = $"AJU-{ajuste.Fecha:yyyy-MM-dd}";

            var Moviento = RegistrarMovimiento(TipoMovimientos.Ajuste.ToString(), Referencia, -Cantidad, Stock_actual);

            return (ajuste, Moviento);
        }

        [GraphQLIgnore]
        public void ComprometerStock(int cantidad) => Stock_actual -= cantidad;

        [GraphQLIgnore]
        public void DevolverStock(int cantidad) => Stock_actual += cantidad;

        [GraphQLIgnore]
        public InsumoMovimiento CrearMovimientoVenta(string codigo, int cantidad) =>
            RegistrarMovimiento(TipoMovimientos.Venta.ToString(), codigo, -cantidad, Stock_actual);

        [GraphQLIgnore]
        public InsumoMovimiento AjusteVenta(string Codigo, int cantidad)
        {
            Stock_actual -= cantidad;
            return RegistrarMovimiento(TipoMovimientos.Venta.ToString(), Codigo, -cantidad, Stock_actual);
        }

        [GraphQLIgnore]
        public InsumoMovimiento AjusteDescuentoporreceta(int cantidad, string nombre)
        {
            Stock_actual -= cantidad;
            return RegistrarMovimiento(TipoMovimientos.Receta.ToString(), $"Receta-{nombre}", -cantidad, Stock_actual);
        }

        [GraphQLIgnore]
        public InsumoMovimiento Compra(string referencia, int cantidad, decimal precioUnitario)
        {
            Stock_actual += (int)(cantidad * Factor_conversion);
            Costo = precioUnitario;
            return RegistrarMovimiento(TipoMovimientos.Compra.ToString(), referencia, cantidad, Stock_actual);
        }
    }
}
