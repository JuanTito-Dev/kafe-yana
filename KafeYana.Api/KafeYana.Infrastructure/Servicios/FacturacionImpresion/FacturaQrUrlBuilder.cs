using KafeYana.Domain.Entities;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    internal static class FacturaQrUrlBuilder
    {
        public static string Construir(Venta venta)
        {
            var utc = venta.FechaEmision.Kind switch
            {
                DateTimeKind.Utc => venta.FechaEmision,
                DateTimeKind.Local => venta.FechaEmision.ToUniversalTime(),
                _ => DateTime.SpecifyKind(venta.FechaEmision, DateTimeKind.Utc)
            };

            var fechaBolivia = TimeZoneInfo.ConvertTimeFromUtc(utc, SiatFechaEmision.ZonaBolivia);
            var fecha = fechaBolivia.ToString("yyyyMMdd");
            var cuf = Uri.EscapeDataString(venta.Cuf.Trim());

            return $"https://siat.impuestos.gob.bo/consulta/QR?nit={venta.NitEmisor}&cuf={cuf}&numero={venta.NumeroFactura}&fecha={fecha}";
        }
    }
}
