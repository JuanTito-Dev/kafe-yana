using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ElaboradosDtos
{
    public class DtoElaboradoCliente
    {
        [Required(ErrorMessage = "Nombre requerido")]
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Precio requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Precio no puedo ser 0 o menos")]
        public required decimal Precio { get; set; }

        //Fk for categoria 

        [Required(ErrorMessage = "Categoria nesesaria")]
        public required int Categoria_Id { get; set; }

        [Required]
        public required string Unidad_medida { get; set; }

        [Required]
        public bool Producible { get; set; } = false;

        public Producto CrearEntidad()
        {
            var producto = new Producto
            {
                Nombre = this.Nombre,
                Descripcion = this.Descripcion,
                Precio = this.Precio,
                Tipo = TiposProductos.Elaborado,
                Categoria_Id = this.Categoria_Id,
                Elaborado = new Elaborado
                {
                    Unidad_medida = this.Unidad_medida,
                    Producible = this.Producible
                }

            };

            return producto;
        }

        public void Editar(Producto datos)
        {
            datos.Nombre = this.Nombre;
            datos.Descripcion = this.Descripcion;
            datos.Precio = this.Precio;
            datos.Categoria_Id = this.Categoria_Id;
            datos.Elaborado.Unidad_medida = this.Unidad_medida;
            datos.Elaborado.Producible = this.Producible;
        }
    }
}
