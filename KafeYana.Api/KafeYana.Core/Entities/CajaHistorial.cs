using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class CajaHistorial : BaseEntity
    {
        public required string Codigo { get; set; }
        public required DateTime Apertura { get; set; }
        public required DateTime Cierre { get; set; }
        public required decimal SaldoInicial { get; set; }
        public required decimal TotalIngresos { get; set; }
        public required decimal TotalEgresos { get; set; }
        public required decimal TotalVentas { get; set; }
        public decimal TotalEfectivo { get; set; } = 0;
        public decimal TotalTarjeta { get; set; } = 0;
        public decimal TotalQr { get; set; } = 0;
        public required decimal Diferencia { get; set; }
        public required string Estado { get; set; }
        public string Nota { get; set; } = string.Empty;
        public string AbiertaPor { get; set; } = string.Empty;
        public string? CerradaPor { get; set; }
        public List<CajaHistorialMovimiento> Movimientos { get; set; } = new List<CajaHistorialMovimiento>();
    }
}
