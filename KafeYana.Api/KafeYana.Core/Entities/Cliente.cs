using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Cliente : BaseEntity
    {
        public int? Dni { get; set; }
        public required string Nombre { get; set; }

        public required string Celular { get; set; }  

        public string? Correo { get; set; }  

        public string? Correonormalizado { get; set; } 

        public DateTime? Fecha_nacimiento { get; set; }

        public string? Direccion { get; set; }

        public int Puntos { get; set; } = 0;

        public bool Estado { get; set; } = true;

        public Pedido? Pedido { get; set; }

    }
}
