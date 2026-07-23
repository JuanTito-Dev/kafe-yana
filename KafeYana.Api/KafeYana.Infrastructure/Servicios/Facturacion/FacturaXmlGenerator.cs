using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using System.Globalization;
using System.Text;
using System.Xml;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class FacturaXmlGenerator : IFacturaXmlGenerator
    {
        private const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
        private const string RootElement = "facturaComputarizadaCompraVenta";

        // Tarjeta (TipoPagos.Tarjeta=2) se declara ante el SIN como
        // "DEBITO AUTOMATICO - OTRO" en vez del código genérico de tarjeta.
        private const int CodigoSiatTarjetaDebitoAutomaticoOtro = 308;

        private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);

        public string Generar(Venta venta)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Utf8SinBom,
                OmitXmlDeclaration = false,
                Indent = false
            };

            using var buffer = new MemoryStream();
            using (var writer = XmlWriter.Create(buffer, settings))
            {
                writer.WriteStartDocument(standalone: true);
                writer.WriteStartElement(RootElement);
                writer.WriteAttributeString("xmlns", "xsi", null, XsiNs);
                writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", XsiNs, "facturaComputarizadaCompraVenta.xsd");

                EscribirCabecera(writer, venta);

                foreach (var detalle in venta.Detalles)
                    EscribirDetalle(writer, detalle);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return Utf8SinBom.GetString(buffer.ToArray());
        }

        private static void EscribirCabecera(XmlWriter writer, Venta venta)
        {
            writer.WriteStartElement("cabecera");

            EscribirElemento(writer, "nitEmisor", venta.NitEmisor.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "razonSocialEmisor", venta.RazonSocialEmisor);
            EscribirElemento(writer, "municipio", venta.Municipio);
            EscribirOpcional(writer, "telefono", venta.Telefono);
            EscribirElemento(writer, "numeroFactura", venta.NumeroFactura!.Value.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "cuf", venta.Cuf);
            EscribirElemento(writer, "cufd", venta.Cufd);
            EscribirElemento(writer, "codigoSucursal", venta.CodigoSucursal.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "direccion", venta.Direccion);

            EscribirElemento(writer, "codigoPuntoVenta", venta.CodigoPuntoVenta.ToString(CultureInfo.InvariantCulture));

            EscribirElemento(writer, "fechaEmision", SiatFechaEmision.Formatear(venta.FechaEmision));
            EscribirOpcional(writer, "nombreRazonSocial", venta.NombreRazonSocial);
            EscribirElemento(writer, "codigoTipoDocumentoIdentidad", venta.CodigoTipoDocumentoIdentidad.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "numeroDocumento", venta.NumeroDocumento);

            if (venta.ComplementoEsNuloSiat)
                EscribirNulo(writer, "complemento");
            else
                EscribirElemento(writer, "complemento", venta.Complemento!);

            EscribirElemento(writer, "codigoCliente", venta.CodigoCliente);
            var codigoMetodoPagoSiat = venta.CodigoMetodoPago == (int)TipoPagos.Tarjeta
                ? CodigoSiatTarjetaDebitoAutomaticoOtro
                : venta.CodigoMetodoPago;
            EscribirElemento(writer, "codigoMetodoPago", codigoMetodoPagoSiat.ToString(CultureInfo.InvariantCulture));

            if (SiatXmlHelper.EsNulo(venta.NumeroTarjeta))
                EscribirNulo(writer, "numeroTarjeta");
            else
                EscribirElemento(writer, "numeroTarjeta", venta.NumeroTarjeta!);

            EscribirElemento(writer, "montoTotal", FormatoNumero(venta.MontoTotal));
            EscribirElemento(writer, "montoTotalSujetoIva", FormatoNumero(venta.MontoTotalSujetoIva));
            EscribirElemento(writer, "codigoMoneda", venta.CodigoMoneda.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "tipoCambio", FormatoNumero(venta.TipoCambio));
            EscribirElemento(writer, "montoTotalMoneda", FormatoNumero(venta.MontoTotalMoneda));

            if (venta.MontoGiftCard is null)
                EscribirNulo(writer, "montoGiftCard");
            else
                EscribirElemento(writer, "montoGiftCard", FormatoNumero(venta.MontoGiftCard.Value));

            if (venta.DescuentoAdicional is null)
                EscribirNulo(writer, "descuentoAdicional");
            else
                EscribirElemento(writer, "descuentoAdicional", FormatoNumero(venta.DescuentoAdicional.Value));

            if (venta.CodigoExcepcion is null or 0)
                EscribirNulo(writer, "codigoExcepcion");
            else
                EscribirElemento(writer, "codigoExcepcion", venta.CodigoExcepcion.Value.ToString(CultureInfo.InvariantCulture));

            if (SiatXmlHelper.EsNulo(venta.Cafc))
                EscribirNulo(writer, "cafc");
            else
                EscribirElemento(writer, "cafc", venta.Cafc!);

            EscribirElemento(writer, "leyenda", venta.Leyenda);
            EscribirElemento(writer, "usuario", venta.Usuario);
            EscribirElemento(writer, "codigoDocumentoSector", venta.CodigoDocumentoSector.ToString(CultureInfo.InvariantCulture));

            writer.WriteEndElement();
        }

        private static void EscribirDetalle(XmlWriter writer, Detalle_Pago detalle)
        {
            writer.WriteStartElement("detalle");

            EscribirElemento(writer, "actividadEconomica", detalle.ActividadEconomica);
            EscribirElemento(writer, "codigoProductoSin", detalle.CodigoProductoSin.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "codigoProducto", detalle.CodigoProducto);
            EscribirElemento(writer, "descripcion", detalle.Descripcion);
            EscribirElemento(writer, "cantidad", FormatoNumero(detalle.Cantidad));
            EscribirElemento(writer, "unidadMedida", detalle.UnidadMedida.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "precioUnitario", FormatoNumero(detalle.PrecioUnitario));

            if (detalle.MontoDescuento is null)
                EscribirElemento(writer, "montoDescuento", "0");
            else
                EscribirElemento(writer, "montoDescuento", FormatoNumero(detalle.MontoDescuento.Value));

            EscribirElemento(writer, "subTotal", FormatoNumero(detalle.SubTotal));

            if (SiatXmlHelper.EsNulo(detalle.NumeroSerie))
                EscribirNulo(writer, "numeroSerie");
            else
                EscribirElemento(writer, "numeroSerie", detalle.NumeroSerie!);

            if (SiatXmlHelper.EsNulo(detalle.NumeroImei))
                EscribirNulo(writer, "numeroImei");
            else
                EscribirElemento(writer, "numeroImei", detalle.NumeroImei!);

            writer.WriteEndElement();
        }

        private static void EscribirElemento(XmlWriter writer, string nombre, string valor)
        {
            writer.WriteStartElement(nombre);
            writer.WriteString(valor);
            writer.WriteEndElement();
        }

        private static void EscribirOpcional(XmlWriter writer, string nombre, string? valor)
        {
            if (SiatXmlHelper.EsNulo(valor))
                EscribirNulo(writer, nombre);
            else
                EscribirElemento(writer, nombre, valor!);
        }

        private static void EscribirNulo(XmlWriter writer, string nombre)
        {
            writer.WriteStartElement(nombre);
            writer.WriteAttributeString("xsi", "nil", XsiNs, "true");
            writer.WriteEndElement();
        }

        private static string FormatoNumero(decimal valor) =>
            valor.ToString(valor % 1m == 0m ? "0" : "0.00", CultureInfo.InvariantCulture);
    }
}
