using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public class DtoProductosLista
    {
        [Required]
        public required string Codigo { get; set; }

        public string CodigoAux { get; set; } = string.Empty;

        public string CodigoAux2 { get; set; } = string.Empty;
        public required string Nombre { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Unidad_Medida { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "No puedes inresar menos de 0")]
        public required int Stock_Actual { get; set; }

        public int Stock_Minimo { get; set; } = 0;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "No puedes inresar menos de 0")]
        public required decimal Costo { get; set; } = 1;

        public decimal Precio { get; set; } = 0;


        public virtual Producto Crear()
        {
            return new Producto
            {
                Codigo = Codigo,
                CodigoAux = this.CodigoAux != string.Empty ? this.CodigoAux : string.Empty,
                CodigoAux2 = this.CodigoAux2 != string.Empty ? this.CodigoAux2 : string.Empty,
                Nombre = this.Nombre != string.Empty ? this.Nombre : this.Codigo,
                Marca = this.Marca != string.Empty ? this.Marca : string.Empty,
                Ubicacion = this.Ubicacion != string.Empty ? this.Ubicacion : string.Empty, 
                Descripcion = this.Descripcion != string.Empty ? this.Descripcion : string.Empty,
                Unidad_Medida = this.Unidad_Medida != string.Empty ? this.Unidad_Medida : string.Empty,
                Stock_Actual = this.Stock_Actual,
                Stock_Minimo = this.Stock_Minimo <= 0 ? 5 : this.Stock_Minimo,
                Costo = this.Costo,
                Precio = this.Precio > 0? this.Precio : 0,
                ConversionABs = 6.69M,

            }; 
        }


        public virtual void Actualizar(Producto producto)
        {
            producto.CodigoAux = this.CodigoAux != string.Empty? CodigoAux : producto.CodigoAux;
            producto.CodigoAux2 = this.CodigoAux2 != string.Empty ? this.CodigoAux2 : producto.CodigoAux2;
            producto.Nombre = this.Nombre != string.Empty ? this.Nombre : producto.Nombre;
            producto.Marca = this.Marca != string.Empty ? this.Marca : producto.Marca;
            producto.Descripcion = this.Descripcion != string.Empty ? this.Descripcion : producto.Descripcion;
            producto.Unidad_Medida = this.Unidad_Medida != string.Empty ? this.Unidad_Medida : producto.Unidad_Medida;
            producto.Stock_Actual += this.Stock_Actual;
            producto.Stock_Minimo = this.Stock_Minimo <= 0 ? producto.Stock_Minimo : this.Stock_Minimo;
            producto.Costo = this.Costo <= 0? producto.Costo : this.Costo;
            producto.Precio = this.Precio > 0? this.Precio : producto.Precio;
            producto.Ubicacion = this.Ubicacion != string.Empty ? this.Ubicacion: producto.Ubicacion; 
        }
    }
}
