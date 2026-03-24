using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.CompradoDtos
{
    public class DtoCompradoCrear
    {
        [Required(ErrorMessage = "Nombre es requerido")]
        public required string Nombre { get; set; }

        [MaxLength(50)]
        public string Codigo_barra { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoria es requerida")]
        public required int Categoria_Id { get; set; }

        [Required(ErrorMessage = "Unidad de medida requerida")]
        public required string Unidad_medida { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;


        [Required(ErrorMessage = "Costo compra requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0")]
        public required decimal Costo_compra { get; set; }

        [Required(ErrorMessage = "Precio venta requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public required decimal Precio { get; set; }

        [Required(ErrorMessage = "Stock actual requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public required int Stock_actual { get; set; }

        [Required(ErrorMessage = "Stock minimo requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public required int Stock_minimo { get; set; }

        [Required]
        public required bool Disponible { get; set; }


        public Producto ProductoCrear()
        {
            var proudcto = new Producto
            {
                Nombre = this.Nombre,
                Descripcion = this.Descripcion,
                Precio = this.Precio,
                Tipo = TiposProductos.Comprado,
                Categoria_Id = this.Categoria_Id,
                Comprado = new Comprado
                {
                    Codigo_barra = this.Codigo_barra,
                    Unidad_medida = this.Unidad_medida,
                    Marca = this.Marca,
                    Ubicacion = this.Ubicacion,
                    Costo_compra = this.Costo_compra,
                    Stock_actual = this.Stock_actual,
                    Stock_minimo = this.Stock_minimo,
                    Disponible = this.Disponible,
                }
            };

            return proudcto;
        }

        public void Editar(Producto datos)
        {
            datos.Nombre = this.Nombre;
            datos.Descripcion = this.Descripcion;
            datos.Precio = this.Precio;
            datos.Categoria_Id = this.Categoria_Id;
            datos.Comprado.Codigo_barra = this.Codigo_barra;
            datos.Comprado.Unidad_medida = this.Unidad_medida;
            datos.Comprado.Marca = this.Marca;
            datos.Comprado.Ubicacion = this.Ubicacion;
            datos.Comprado.Costo_compra = this.Costo_compra;
            datos.Comprado.Stock_actual = this.Stock_actual;
            datos.Comprado.Stock_minimo = this.Stock_minimo;
            datos.Comprado.Disponible  = this.Disponible;
        }
    }
}
