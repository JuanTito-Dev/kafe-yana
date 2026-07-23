using HotChocolate;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Caja : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public required decimal SaldoInicial {  get;  set; }

        public decimal TotalVentas { get; private set; } = 0.00M;

        public decimal TotalEfectivo { get; private set; } = 0.00M;

        public decimal TotalTarjeta { get; private set; } = 0.00M;

        public decimal TotalQr { get; private set; } = 0.00M;

        public decimal TotalIngresos { get; private set; } = 0.00M;

        public decimal TotalEgresos { get; private set; } = 0.00M;

        public decimal SaldoEsperado => SaldoInicial + TotalEfectivo + TotalIngresos + TotalEgresos;

        public bool Abierta { get; set; } = true;
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
        public DateTime? FechaCierre { get; set; }
        public string AbiertaPor { get; set; } = string.Empty;
        public string? CerradaPor { get; set; }

        public List<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();

        [GraphQLIgnore]
        public CajaMovimiento CajaIngreso(decimal cantidad, string Categoria, string descripcion, string Referencia, string Nota)
        {
            TotalIngresos += cantidad;

            return new CajaMovimiento
            {
                Id_Caja = Id,
                Fecha = DateTime.UtcNow,
                Tipo = MovimientosCaja.Ingreso,
                Categoria = Categoria,
                Descripcion = descripcion,
                Referencia = Referencia,
                Monto = cantidad,
                Nota = Nota

            };
        }

        [GraphQLIgnore]
        public CajaMovimiento CajaEgresos(decimal Cantidad, string Categoria, string descripcion, string Referencia, string Nota)
        {
            TotalEgresos -= Cantidad;

            return new CajaMovimiento
            {
                Id_Caja = Id,
                Fecha = DateTime.UtcNow,
                Tipo = MovimientosCaja.Engreso,
                Categoria = Categoria,
                Descripcion = descripcion,
                Referencia = Referencia,
                Monto = -Cantidad,
                Nota = Nota
            };
        }

        [GraphQLIgnore]
        public void RegistrarVenta(decimal efectivo, decimal tarjeta, decimal qr)
        {
            TotalEfectivo += efectivo;
            TotalTarjeta += tarjeta;
            TotalQr += qr;
            TotalVentas += efectivo + tarjeta + qr;
        }

        /// <summary>
        /// Sobrecarga que toma la lista de líneas de pago del DTO
        /// (<c>DtoPagos.Lineas</c>) y suma al acumulador correspondiente según
        /// el código SIN del método. Mantiene los totales de caja correctos sin
        /// tener que conocer la estructura fija anterior (Efectivo/Tarjeta/Qr).
        /// </summary>
        [GraphQLIgnore]
        public void RegistrarVenta(IEnumerable<(int CodigoMetodoPago, decimal Monto)> lineas)
        {
            decimal ef = 0, tj = 0, qr = 0;
            foreach (var (codigo, monto) in lineas)
            {
                if (monto <= 0) continue;
                switch (codigo)
                {
                    case 1: ef += monto; break;             // EFECTIVO
                    case 2: tj += monto; break;             // TARJETA
                    case 7: qr += monto; break;             // TRANSFERENCIA BANCARIA (QR)
                    default: ef += monto; break;            // Otros → se imputan a efectivo por simplicidad de caja.
                }
            }
            RegistrarVenta(ef, tj, qr);
        }

        [GraphQLIgnore]
        public CajaMovimiento RegistrarReembolso(decimal monto, TipoPagos tipoPago, string motivo, string referencia)
        {
            switch (tipoPago)
            {
                case TipoPagos.Efectivo: TotalEfectivo -= monto; break;
                case TipoPagos.Tarjeta: TotalTarjeta  -= monto; break;
                case TipoPagos.Transferencia: TotalQr -= monto; break;
            }

            return new CajaMovimiento
            {
                Id_Caja     = Id,
                Fecha       = DateTime.UtcNow,
                Tipo        = MovimientosCaja.Engreso,
                Categoria   = "Reembolso",
                Descripcion = $"Reembolso {tipoPago} - venta {referencia}",
                Referencia  = referencia,
                Monto       = -monto,
                Nota        = motivo
            };
        }

        [GraphQLIgnore]
        public CajaHistorial CerrarCaja(decimal montoFinal, string cerradaPor, string nota)
        {
            Abierta = false;
            FechaCierre = DateTime.UtcNow;
            CerradaPor = cerradaPor;

            var diferencia = montoFinal - SaldoEsperado;
            var estado = diferencia == 0 ? "Sin diferencia" : diferencia > 0 ? "Sobrante" : "Faltante";

            return new CajaHistorial
            {
                Codigo = Nombre,
                Apertura = FechaApertura,
                Cierre = FechaCierre.Value,
                SaldoInicial = SaldoInicial,
                TotalIngresos = TotalIngresos,
                TotalEgresos = TotalEgresos,
                TotalVentas = TotalVentas,
                TotalEfectivo = TotalEfectivo,
                TotalTarjeta = TotalTarjeta,
                TotalQr = TotalQr,
                Diferencia = diferencia,
                Estado = estado,
                Nota = nota,
                AbiertaPor = this.AbiertaPor,
                CerradaPor = cerradaPor,
                Movimientos = Movimientos.Select(m => new CajaHistorialMovimiento
                {
                    Codigo = Nombre,
                    Categoria = m.Categoria,
                    Tipo = m.Tipo,
                    Descripcion = m.Descripcion,
                    Monto = m.Monto
                }).ToList()
            };
        }
    }
}
