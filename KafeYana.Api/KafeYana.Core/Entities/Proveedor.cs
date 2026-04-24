using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Proveedor : BaseEntity
    {
        public required string Razon_Social { get; set; }

        public string Dni { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Celular { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;
    }
}
