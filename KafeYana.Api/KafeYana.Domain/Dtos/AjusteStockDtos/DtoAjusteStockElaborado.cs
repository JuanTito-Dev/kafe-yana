using KafeYana.Core.Entities.Entity;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.AjusteStockDtos
{
    public class DtoAjusteStockElaborado 
    {
        [Required]
        public required int Id_elaborado { get; set; }

        [Required]
        public int Cantidad { get; set; } = 1;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string Motivo { get; set; } = string.Empty;

        public string Nota { get; set; } = string.Empty;


        public Stock_Ajuste Crear(string Nombre, string NombreUsuario)
        {
            return new Stock_Ajuste
            {
                Nombre = Nombre,
                Fecha = this.Fecha,
                Motivo = this.Motivo,
                Nota = this.Nota,
                Usuario = NombreUsuario,
                Tipo = "Elaborado",
                Perdida = 0
            };
        }
    }
}
