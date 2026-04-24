using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Stock_Ajuste : BaseEntity
    {
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public required string Nombre { get; set; } 

        public required string Tipo { get; set; }

        public int Ajuste { get; set; }

        public int StockAnterior { get; set; }

        public int StockNuevo { get; set; }

        public decimal Perdida { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string Nota { get; set; } = string.Empty;

        public required string Usuario { get; set; } 
    }
}
