using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using System.Globalization;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    internal sealed class FacturaTicketBuilder(int anchoCaracteres)
    {
        private static readonly byte[] Init = [0x1B, 0x40];
        private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
        private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
        private static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01];
        private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
        private static readonly byte[] Normal = [0x1D, 0x21, 0x00];
        private static readonly byte[] Cut = [0x1D, 0x56, 0x41, 0x10];
        private static readonly byte[] Lf = [0x0A];

        private static readonly Encoding Enc = Encoding.GetEncoding("iso-8859-1");

        public byte[] Construir(Venta venta, string urlQr)
        {
            using var ms = new MemoryStream();
            ms.Write(Init);

            EscribirCentrado(ms, venta.RazonSocialEmisor, bold: true);
            EscribirCentrado(ms, EtiquetaSucursal(venta.CodigoSucursal));
            EscribirCentrado(ms, $"No. Punto de Venta {venta.CodigoPuntoVenta}");
            EscribirCentrado(ms, venta.Direccion);
            if (!string.IsNullOrWhiteSpace(venta.Telefono))
                EscribirCentrado(ms, $"Telefono: {venta.Telefono}");
            EscribirCentrado(ms, venta.Municipio);
            EscribirLinea(ms);

            EscribirCentrado(ms, $"NIT: {venta.NitEmisor}");
            EscribirLinea(ms);
            EscribirCentrado(ms, "FACTURA", bold: true);
            EscribirCentrado(ms, "(Con Derecho a Credito Fiscal)", bold: true);
            EscribirLinea(ms);

            EscribirIzq(ms, $"FACTURA Nro.: {venta.NumeroFactura}", bold: true);
            EscribirIzq(ms, "COD. AUTORIZACION:", bold: true);
            foreach (var parte in PartirTexto(venta.Cuf, anchoCaracteres))
                EscribirIzq(ms, parte);

            EscribirIzq(ms, $"Fecha: {FormatearFecha(venta.FechaEmision)}");
            EscribirIzq(ms, $"Nombre/Razon Social: {venta.NombreRazonSocial ?? "-"}");
            EscribirIzq(ms, $"NIT/CI/CEX: {EtiquetaDocumento(venta.CodigoTipoDocumentoIdentidad)} {venta.NumeroDocumento}");
            if (!string.IsNullOrWhiteSpace(venta.Complemento))
                EscribirIzq(ms, $"Complemento: {venta.Complemento}");
            EscribirIzq(ms, $"Cod. Cliente: {venta.CodigoCliente}");
            EscribirLinea(ms);

            EscribirIzq(ms, CabeceraDetalle(), bold: true);
            EscribirLinea(ms);

            decimal subtotalLineas = 0;
            decimal descuentoLineas = 0;

            foreach (var item in venta.Detalles)
            {
                var desc = item.MontoDescuento ?? 0m;
                subtotalLineas += item.SubTotal;
                descuentoLineas += desc;

                EscribirIzq(ms, $"Cod: {item.CodigoProducto}");
                EscribirIzq(ms, $"Cant: {FormatoNumero(item.Cantidad)}  UM: {EtiquetaUnidad(item.UnidadMedida)}");
                foreach (var linea in PartirTexto(item.Descripcion, anchoCaracteres - 2))
                    EscribirIzq(ms, $"  {linea}");
                EscribirPar(ms, "P.Unit:", $"Bs/{FormatoNumero(item.PrecioUnitario)}");
                EscribirPar(ms, "Desc:", $"Bs/{FormatoNumero(desc)}");
                EscribirPar(ms, "Subtotal:", $"Bs/{FormatoNumero(item.SubTotal)}", bold: true);
                EscribirLinea(ms, '-');
            }

            var descuentoAdicional = venta.DescuentoAdicional ?? 0m;
            var giftCard = venta.MontoGiftCard ?? 0m;

            EscribirPar(ms, "SUBTOTAL Bs:", $"Bs/{FormatoNumero(subtotalLineas)}", bold: true);
            EscribirPar(ms, "DESCUENTO Bs:", $"Bs/{FormatoNumero(descuentoLineas + descuentoAdicional)}");
            EscribirPar(ms, "TOTAL Bs:", $"Bs/{FormatoNumero(venta.MontoTotal)}", bold: true);
            EscribirPar(ms, "MONTO GIFT CARD Bs:", $"Bs/{FormatoNumero(giftCard)}");
            EscribirPar(ms, "MONTO A PAGAR Bs:", $"Bs/{FormatoNumero(venta.MontoTotal)}", bold: true);
            EscribirPar(ms, "IMPORTE BASE CREDITO FISCAL:", $"Bs/{FormatoNumero(venta.MontoTotalSujetoIva)}");
            EscribirLinea(ms);

            EscribirIzq(ms, $"Son: {MontoEnLetrasBoliviano.Formatear(venta.MontoTotal)}");
            EscribirIzq(ms, $"Metodo de pago: {EtiquetaMetodoPago(venta.CodigoMetodoPago)}");
            EscribirLinea(ms);

            foreach (var linea in PartirTexto(venta.Leyenda, anchoCaracteres))
                EscribirIzq(ms, linea);

            EscribirLinea(ms);
            foreach (var linea in PartirTexto(
                "ESTA FACTURA CONTRIBUYE AL DESARROLLO DEL PAIS, EL USO ILICITO SERA SANCIONADO PENALMENTE DE ACUERDO A LEY",
                anchoCaracteres))
                EscribirIzq(ms, linea);

            foreach (var linea in PartirTexto(
                "Este documento es la Representacion Grafica de un Documento Fiscal Digital emitido en una modalidad de facturacion en linea",
                anchoCaracteres))
                EscribirIzq(ms, linea);

            if (!string.IsNullOrWhiteSpace(venta.CodigoRecepcion))
                EscribirIzq(ms, $"Cod. Recepcion SIAT: {venta.CodigoRecepcion}");

            if (venta.EstadoSiat is not null)
                EscribirIzq(ms, $"Estado SIAT: {(int)venta.EstadoSiat}");

            EscribirLinea(ms);
            ms.Write(AlignCenter);
            FacturaEscPosQr.Escribir(ms, urlQr);
            ms.Write(Lf);
            ms.Write(Lf);
            ms.Write(Cut);

            return ms.ToArray();
        }

        private static string CabeceraDetalle() =>
            "CODIGO | CANT | UM | DESCRIPCION | PRECIO | DESC | SUBT";

        private static string EtiquetaSucursal(int codigo) =>
            codigo == 0 ? "SUCURSAL CASA MATRIZ" : $"SUCURSAL N. {codigo}";

        private static string FormatearFecha(DateTime fecha)
        {
            var utc = fecha.Kind switch
            {
                DateTimeKind.Utc => fecha,
                DateTimeKind.Local => fecha.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
            };
            var bolivia = TimeZoneInfo.ConvertTimeFromUtc(utc, SiatFechaEmision.ZonaBolivia);
            return bolivia.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
        }

        private static string EtiquetaDocumento(int codigo)
        {
            // Lee del catálogo sincronizado (catálogo SIAT vigente, con fallback
            // hardcoded mientras el primer sync no haya corrido). Ver
            // TipoDocumentoIdentidadSiatCatalogo.
            var descripcion = TipoDocumentoIdentidadSiatCatalogo.ObtenerDescripcion(codigo);
            if (string.IsNullOrEmpty(descripcion) || descripcion == $"Tipo {codigo}")
                return codigo.ToString(CultureInfo.InvariantCulture);

            // El SIN devuelve descripciones con formato "PREFIJO - RESTO".
            // Para el ticket solo queremos el prefijo corto ("CI", "CEX", etc.).
            return descripcion.Split('-')[0].Trim();
        }

        private static string EtiquetaUnidad(int codigo)
        {
            var item = UnidadMedidaSiatService.Listar().FirstOrDefault(x => x.Codigo == codigo);
            return item?.Descripcion ?? codigo.ToString(CultureInfo.InvariantCulture);
        }

        private static string EtiquetaMetodoPago(int codigo) => codigo switch
        {
            (int)TipoPagos.Efectivo => "EFECTIVO",
            (int)TipoPagos.Tarjeta => "TARJETA",
            (int)TipoPagos.Transferencia => "QR / TRANSFERENCIA",
            _ => "OTROS"
        };

        private static string FormatoNumero(decimal valor) =>
            valor.ToString(valor % 1m == 0m ? "0" : "0.00", CultureInfo.InvariantCulture);

        private void EscribirCentrado(MemoryStream ms, string texto, bool bold = false)
        {
            ms.Write(AlignCenter);
            if (bold) ms.Write(BoldOn);
            ms.Write(Enc.GetBytes(texto));
            ms.Write(Lf);
            if (bold) ms.Write(BoldOff);
            ms.Write(AlignLeft);
        }

        private void EscribirIzq(MemoryStream ms, string texto, bool bold = false)
        {
            ms.Write(AlignLeft);
            if (bold) ms.Write(BoldOn);
            foreach (var linea in PartirTexto(texto, anchoCaracteres))
            {
                ms.Write(Enc.GetBytes(linea));
                ms.Write(Lf);
            }
            if (bold) ms.Write(BoldOff);
        }

        private void EscribirPar(MemoryStream ms, string izq, string der, bool bold = false)
        {
            var pad = Math.Max(1, anchoCaracteres - izq.Length - der.Length);
            EscribirIzq(ms, izq + new string(' ', pad) + der, bold);
        }

        private void EscribirLinea(MemoryStream ms, char c = '=')
        {
            ms.Write(Enc.GetBytes(new string(c, anchoCaracteres)));
            ms.Write(Lf);
        }

        private static IEnumerable<string> PartirTexto(string texto, int ancho)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                yield return string.Empty;
                yield break;
            }

            var palabras = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var linea = new StringBuilder();

            foreach (var palabra in palabras)
            {
                if (palabra.Length > ancho)
                {
                    if (linea.Length > 0)
                    {
                        yield return linea.ToString();
                        linea.Clear();
                    }
                    for (var i = 0; i < palabra.Length; i += ancho)
                    {
                        var len = Math.Min(ancho, palabra.Length - i);
                        yield return palabra.Substring(i, len);
                    }
                    continue;
                }

                if (linea.Length + palabra.Length + 1 > ancho)
                {
                    yield return linea.ToString().TrimEnd();
                    linea.Clear();
                }

                if (linea.Length > 0)
                    linea.Append(' ');
                linea.Append(palabra);
            }

            if (linea.Length > 0)
                yield return linea.ToString().TrimEnd();
        }
    }
}
