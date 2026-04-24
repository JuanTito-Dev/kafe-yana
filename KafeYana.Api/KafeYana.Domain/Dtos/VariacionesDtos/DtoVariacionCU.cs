using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.VariacionesDtos
{
    public class DtoVariacionCU
    {
        [Required(ErrorMessage = "El nombre de la variación es obligatorio.")]
        public required string Nombre { get; set; }

        public required bool Requerido { get; set; } = false;

        [Required]
        public required int Id_Producto { get; set; }

        public Variacion Crear()
        {
            var variacion = new Variacion
            {
                Nombre = Nombre,
                Requerido = Requerido,
                Id_Elaborado = Id_Producto
            };
            return variacion;
        }

        public void Actualizar(Variacion variacion)
        {
            variacion.Nombre = Nombre;
            variacion.Requerido = Requerido;
            variacion.Id_Elaborado = Id_Producto;
        }
    }
}
