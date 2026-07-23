using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using System.Globalization;
using System.Text;
using System.Xml;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Genera el XML del archivo que se gzip-ea y se envía en el campo "archivo" del
    /// SOAP de "recepcionDocumentoAjuste". Estructura y orden de campos espejados de
    /// scripts/gen_siat_ajuste.py.
    ///
    /// Bifurcación por sector (válido para piloto/producción según catálogo SIN):
    ///
    /// • sector 24 (NCD — Nota Crédito/Débito genérica)
    ///     Raíz: <c>&lt;notaFiscalComputarizadaCreditoDebito&gt;</c>
    ///     XSD:  <c>notaComputarizadaCreditoDebito.xsd</c>
    ///     Cabecera SIN <c>&lt;descuentoAdicional&gt;</c>.
    ///     Detalle SIN <c>&lt;nroItem&gt;</c>.
    ///
    /// • sector 47 (NCDDE — Nota Débito por Devolución / Descuentos Posteriores)
    ///     Raíz: <c>&lt;notaComputarizadaCreditoDebitoDescuento&gt;</c>
    ///     XSD:  <c>notaComputarizadaCreditoDebitoDescuento.xsd</c>
    ///     Cabecera CON <c>&lt;descuentoAdicional&gt;</c> (obligatorio — ver
    ///     scripts/muchacho.py:109).
    ///     Detalle CON <c>&lt;nroItem&gt;</c> correlativo 1..N como primer hijo.
    ///
    /// El SIAT escoge el XSD a aplicar según el <c>codigoDocumentoSector</c> de la
    /// cabecera; enviar la raíz equivocada dispara error 920
    /// "Cannot find the declaration of element 'notaFiscalComputarizadaCreditoDebito'".
    /// </summary>
    public class NotaAjusteXmlGenerator : INotaAjusteXmlGenerator
    {
        private const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";

        // Sector 24 — raíz y XSD originales
        private const string RootElementSector24 = "notaFiscalComputarizadaCreditoDebito";
        private const string XsdLocationSector24 = "notaComputarizadaCreditoDebito.xsd";

        // Sector 47 — raíz y XSD para descuentos / devoluciones
        private const string RootElementSector47 = "notaComputarizadaCreditoDebitoDescuento";
        private const string XsdLocationSector47 = "notaComputarizadaCreditoDebitoDescuento.xsd";

        private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);

        public string Generar(NotaAjuste nota)
        {
            var (rootElement, xsdLocation) = nota.CodigoDocumentoSector == 47
                ? (RootElementSector47, XsdLocationSector47)
                : (RootElementSector24, XsdLocationSector24);

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
                writer.WriteStartElement(rootElement);
                writer.WriteAttributeString("xmlns", "xsi", null, XsiNs);
                writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", XsiNs, xsdLocation);

                EscribirCabecera(writer, nota);

                foreach (var detalle in nota.Detalles)
                    EscribirDetalle(writer, detalle, nota.CodigoDocumentoSector);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return Utf8SinBom.GetString(buffer.ToArray());
        }

        private static void EscribirCabecera(XmlWriter writer, NotaAjuste nota)
        {
            writer.WriteStartElement("cabecera");

            EscribirElemento(writer, "nitEmisor", nota.NitEmisor.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "razonSocialEmisor", nota.RazonSocialEmisor);
            EscribirElemento(writer, "municipio", nota.Municipio);
            EscribirOpcional(writer, "telefono", nota.Telefono);
            EscribirElemento(writer, "numeroNotaCreditoDebito", nota.NumeroNotaCreditoDebito.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "cuf", nota.Cuf);
            EscribirElemento(writer, "cufd", nota.Cufd);
            EscribirElemento(writer, "codigoSucursal", nota.CodigoSucursal.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "direccion", nota.Direccion);

            EscribirElemento(writer, "codigoPuntoVenta", nota.CodigoPuntoVenta.ToString(CultureInfo.InvariantCulture));

            EscribirElemento(writer, "fechaEmision", SiatFechaEmision.Formatear(nota.FechaEmision));
            EscribirOpcional(writer, "nombreRazonSocial", nota.NombreRazonSocial);
            EscribirElemento(writer, "codigoTipoDocumentoIdentidad", nota.CodigoTipoDocumentoIdentidad.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "numeroDocumento", nota.NumeroDocumento);

            if (nota.ComplementoEsNuloSiat)
                EscribirNulo(writer, "complemento");
            else
                EscribirElemento(writer, "complemento", nota.Complemento!);

            EscribirElemento(writer, "codigoCliente", nota.CodigoCliente);

            // Referencia a la factura original
            EscribirElemento(writer, "numeroFactura", nota.NumeroFacturaOriginal.ToString(CultureInfo.InvariantCulture));
            EscribirElemento(writer, "numeroAutorizacionCuf", nota.NumeroAutorizacionCuf);
            EscribirElemento(writer, "fechaEmisionFactura", SiatFechaEmision.Formatear(nota.FechaEmisionFactura));

            // Montos
            EscribirElemento(writer, "montoTotalOriginal", FormatoNumero(nota.MontoTotalOriginal));

            // descuentoAdicional: SOLO sector 47. El XSD de sector 24 lo rechaza
            // como elemento desconocido; el XSD de sector 47 lo exige.
            // Ver scripts/muchacho.py:109.
            if (nota.CodigoDocumentoSector == 47)
                EscribirElemento(writer, "descuentoAdicional", FormatoNumero(nota.DescuentoAdicional ?? 0m));

            EscribirElemento(writer, "montoTotalDevuelto", FormatoNumero(nota.MontoTotalDevuelto));
            EscribirElemento(writer, "montoDescuentoCreditoDebito", FormatoNumero(nota.MontoDescuentoCreditoDebito));
            EscribirElemento(writer, "montoEfectivoCreditoDebito", FormatoNumero(nota.MontoEfectivoCreditoDebito));

            if (nota.CodigoExcepcion is null or 0)
                EscribirNulo(writer, "codigoExcepcion");
            else
                EscribirElemento(writer, "codigoExcepcion", nota.CodigoExcepcion.Value.ToString(CultureInfo.InvariantCulture));

            EscribirElemento(writer, "leyenda", nota.Leyenda);
            EscribirElemento(writer, "usuario", nota.Usuario);
            EscribirElemento(writer, "codigoDocumentoSector", nota.CodigoDocumentoSector.ToString(CultureInfo.InvariantCulture));

            writer.WriteEndElement();
        }

        private static void EscribirDetalle(XmlWriter writer, NotaAjusteDetalle detalle, int codigoDocumentoSector)
        {
            writer.WriteStartElement("detalle");

            // nroItem: SOLO sector 47. El XSD de sector 47 lo exige como primer
            // hijo de <detalle>; el de sector 24 lo rechaza como elemento
            // desconocido. Ver scripts/muchacho.py:53-54.
            if (codigoDocumentoSector == 47)
                EscribirElemento(writer, "nroItem", detalle.NroItem.ToString(CultureInfo.InvariantCulture));

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
            EscribirElemento(writer, "codigoDetalleTransaccion", detalle.CodigoDetalleTransaccion.ToString(CultureInfo.InvariantCulture));

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
