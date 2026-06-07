using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Facturacion
{
    public class Cuis : BaseEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public DateTime FechaVigencia { get; set; }
        public int CodigoSucursal { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public bool EsVigente() => DateTime.UtcNow < FechaVigencia;
    }
}
