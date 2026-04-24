using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.RecetaDtos
{
    public class DtoRecetaCU
    {
        [Required(ErrorMessage = "Nombe requerido")]
        public string Nombre { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Las porciones deben ser al menos 1")]
        public int Porciones{ get; set; }

        [Required]
        public int Id_Elaborado { get; set; }

        public IReadOnlyList<DtoDetalle> Detalles { get; set; } = [];

        public Receta Adatar()
        {
            var receta = new Receta
            {
                Nombre = Nombre,
                Nota = Nota,
                Porciones = Porciones,
                Id_Elaborado = Id_Elaborado,
                Detalles = Detalles.Select(x => new Detalle
                {
                    Cantidad = x.Cantidad,
                    Merma = x.Merma,
                    SubTotal = x.SubTotal,
                    Id_insumo = x.Id_insumo

                }).ToList()
            };

            return receta;
        }

        public void Actualizar(Receta receta)
        {
            receta.Nombre = Nombre;
            receta.Nota = Nota;
            receta.Id_Elaborado = Id_Elaborado;
            receta.Porciones = Porciones;
            // Reemplazar los detalles
            receta.Detalles = Detalles.Select(x => new Detalle
            {
                Cantidad = x.Cantidad,
                Merma = x.Merma,
                SubTotal = x.SubTotal,
                Id_insumo = x.Id_insumo
            }).ToList();
        }
    }

    
}
