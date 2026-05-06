using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.BaseEntidades
{
    public class BaseMovimientos : BaseEntity
    {
        public DateTime Fecha { get; protected set; } = DateTime.UtcNow;

        public string Tipo { get;  protected set; } = string.Empty;

        public string Referencia { get; protected set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Costo_Unitario { get; set; } = 0.00M;

        public decimal Total { get; protected set; } = 0.00M;

        public int Stock_resultante { get; set; }

        public BaseMovimientos() { }

        public BaseMovimientos(string Tipo, string Referencia, int Cantidad, decimal Costo, int Stock)
        {
            this.Tipo = Tipo;
            this.Referencia = Referencia;
            this.Cantidad = Cantidad;
            Costo_Unitario = Costo;
            this.Total = Costo * Cantidad;
            this.Stock_resultante = Stock;
        }

    }
}
