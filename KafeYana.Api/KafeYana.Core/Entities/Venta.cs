using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Venta : BaseEntity
    {
        public string Codigo {  get; set; }

        public DateTime Fecha { get; set; }

        public required string Cliente { get; set; }

        public int? Id_Cliente { get; set; }

        public required string Cajero { get; set; }

        public required int Productos { get; set; } = 0;

        public decimal PagoEfectivo { get; set; } = 0;

        public decimal PagoTarjeta { get; set; } = 0;

        public decimal PagoQr { get; set; } = 0;

        public required string Estado {  get; set; }

        public required decimal Subtotal { get; set; }

        public required decimal Total { get; set; }

        public List<Detalle_venta> Detalles { get; set; } = new List<Detalle_venta>();

        public CajaMovimiento Reembolso(Caja caja, decimal monto, string nota)
        {

            Estado = "Reembolsado";
            return caja.CajaEgresos(monto, "Reembolso", nota, Codigo, $"Venta {Codigo}");
        }
    }
}
