using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KafeYana.Infrastructure.SiatClient
{
    /// <summary>
    /// Cliente HTTP para los servicios SOAP del SIAT.
    /// Usa HttpClient directo porque el SIAT requiere el header "apikey"
    /// que WCF/BasicHttpBinding no maneja limpiamente.
    /// </summary>
    public class SiatHttpClient
    {
        private readonly HttpClient _http;
        private readonly SiatOptions _opts;
        private readonly ILogger<SiatHttpClient> _logger;

        // Namespaces SOAP del SIAT (confirmados desde Postman)
        private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NsSiat = "https://siat.impuestos.gob.bo/";
        private static readonly XNamespace SiatNs = NsSiat;
        // WSDL elementFormDefault="unqualified" → hijos sin namespace
        private static readonly XNamespace None = XNamespace.None;

        public SiatHttpClient(
            HttpClient http,
            IOptions<SiatOptions> opts,
            ILogger<SiatHttpClient> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        // CUIS — Código Único de Inicio de Sistema
        // ─────────────────────────────────────────────
        public async Task<RespuestaCuis> SolicitarCuisAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            // Orden WSDL. Operación con prefijo siat:; SolicitudCuis y campos sin namespace.
            var body = new XElement(SiatNs + "cuis",
                Solicitud("SolicitudCuis",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoModalidad", _opts.CodigoModalidad),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionCodigos", body, ct);

            var respEl = BuscarElemento(xml, "RespuestaCuis")
                ?? BuscarElemento(xml, "cuisResponse");

            return new RespuestaCuis
            {
                CodigoCuis = ValorElemento(respEl, "codigoCUIS", "codigo"),
                FechaVigencia = ParseFecha(ValorElemento(respEl, "fechaVigencia")),
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
            };
        }

        // ─────────────────────────────────────────────
        // CUFD — Código Único de Facturación Diaria
        // ─────────────────────────────────────────────
        public async Task<RespuestaCufd> SolicitarCufdAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            // WSDL: operación "cufd" (no "solicitudCufd")
            var body = new XElement(SiatNs + "cufd",
                Solicitud("SolicitudCufd",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoModalidad", _opts.CodigoModalidad),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionCodigos", body, ct);

            var respEl = BuscarElemento(xml, "RespuestaCufd")
                ?? BuscarElemento(xml, "cufdResponse")
                ?? BuscarElemento(xml, "solicitudCufdResponse");

            return new RespuestaCufd
            {
                CodigoCufd = ValorElemento(respEl, "codigoCUFD", "codigo"),
                CodigoControl = ValorElemento(respEl, "codigoControl") ?? string.Empty,
                Direccion = ValorElemento(respEl, "direccion") ?? string.Empty,
                FechaVigencia = ParseFecha(ValorElemento(respEl, "fechaVigencia")),
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
            };
        }

        // ─────────────────────────────────────────────
        // Verificar NIT del cliente
        // ─────────────────────────────────────────────
        public async Task<RespuestaVerificaNit> VerificarNitAsync(
            long nitAVerificar,
            string cuis,
            int codigoSucursal,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "verificarNit",
                Solicitud("SolicitudVerificarNit",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoModalidad", _opts.CodigoModalidad),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit),
                    Campo("nitParaVerificacion", nitAVerificar)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionCodigos", body, ct);

            var respEl = BuscarElemento(xml, "RespuestaVerificarNit")
                ?? BuscarElemento(xml, "verificarNitResponse");

            return new RespuestaVerificaNit
            {
                Transaccion = ParseTransaccion(respEl),
                Mensajes = ParseCodigos(respEl)
            };
        }

        // ─────────────────────────────────────────────
        // Recepción Factura
        // ─────────────────────────────────────────────
        public async Task<RespuestaRecepcionFacturaDto> RecepcionFacturaAsync(
            SolicitudRecepcionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "recepcionFactura",
                Solicitud("SolicitudServicioRecepcionFactura",
                    Campo("codigoAmbiente", solicitud.CodigoAmbiente),
                    Campo("codigoDocumentoSector", solicitud.CodigoDocumentoSector),
                    Campo("codigoEmision", solicitud.CodigoEmision),
                    Campo("codigoModalidad", solicitud.CodigoModalidad),
                    Campo("codigoPuntoVenta", solicitud.CodigoPuntoVenta),
                    Campo("codigoSistema", solicitud.CodigoSistema),
                    Campo("codigoSucursal", solicitud.CodigoSucursal),
                    Campo("cufd", solicitud.Cufd),
                    Campo("cuis", solicitud.Cuis),
                    Campo("nit", solicitud.Nit),
                    Campo("tipoFacturaDocumento", solicitud.TipoFacturaDocumento),
                    Campo("archivo", solicitud.Archivo),
                    Campo("fechaEnvio", FormatearFechaEnvio(solicitud.FechaEnvio)),
                    Campo("hashArchivo", solicitud.HashArchivo)
                )
            );

            var xml = await EnviarSoapAsync(_opts.ServicioRecepcionFactura, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaRecepcion")
                ?? BuscarElemento(xml, "recepcionFacturaResponse");

            return new RespuestaRecepcionFacturaDto
            {
                Transaccion = ParseTransaccion(respEl),
                CodigoEstado = int.TryParse(ValorElemento(respEl, "codigoEstado"), out var estado) ? estado : null,
                CodigoRecepcion = ValorElemento(respEl, "codigoRecepcion"),
                CodigoDescripcion = ValorElemento(respEl, "codigoDescripcion"),
                CodigosRespuesta = ParseCodigos(respEl).Select(c => new CodigoRespuestaSiatDto
                {
                    Codigo = c.Codigo,
                    Descripcion = c.Descripcion
                }).ToList()
            };
        }

        // ─────────────────────────────────────────────
        // Anulación Factura
        // ─────────────────────────────────────────────
        public async Task<RespuestaAnulacionFacturaDto> AnulacionFacturaAsync(
            SolicitudAnulacionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "anulacionFactura",
                Solicitud("SolicitudServicioAnulacionFactura",
                    Campo("codigoAmbiente", solicitud.CodigoAmbiente),
                    Campo("codigoDocumentoSector", solicitud.CodigoDocumentoSector),
                    Campo("codigoEmision", solicitud.CodigoEmision),
                    Campo("codigoModalidad", solicitud.CodigoModalidad),
                    Campo("codigoPuntoVenta", solicitud.CodigoPuntoVenta),
                    Campo("codigoSistema", solicitud.CodigoSistema),
                    Campo("codigoSucursal", solicitud.CodigoSucursal),
                    Campo("cufd", solicitud.Cufd),
                    Campo("cuis", solicitud.Cuis),
                    Campo("nit", solicitud.Nit),
                    Campo("tipoFacturaDocumento", solicitud.TipoFacturaDocumento),
                    Campo("codigoMotivo", solicitud.CodigoMotivo),
                    Campo("cuf", solicitud.Cuf)
                )
            );

            var xml = await EnviarSoapAsync(_opts.ServicioAnulacionFactura, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaAnulacion")
                ?? BuscarElemento(xml, "anulacionFacturaResponse");

            return new RespuestaAnulacionFacturaDto
            {
                Transaccion = ParseTransaccion(respEl),
                CodigoEstado = int.TryParse(ValorElemento(respEl, "codigoEstado"), out var estado) ? estado : null,
                CodigoDescripcion = ValorElemento(respEl, "codigoDescripcion"),
                CodigosRespuesta = ParseCodigos(respEl).Select(c => new CodigoRespuestaSiatDto
                {
                    Codigo = c.Codigo,
                    Descripcion = c.Descripcion
                }).ToList()
            };
        }

        private static string FormatearFechaEnvio(DateTime fecha) =>
            SiatFechaEmision.Formatear(fecha);

        // ─────────────────────────────────────────────
        // Método interno: arma el envelope y envía
        // ─────────────────────────────────────────────
        private async Task<XDocument> EnviarSoapAsync(
            string servicio,
            XElement bodyContent,
            CancellationToken ct)
        {
            // Envelope SOAP estándar con namespace del SIAT
            var envelope = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(XName.Get("Envelope", NsSoapEnv),
                    new XAttribute(XNamespace.Xmlns + "soapenv", NsSoapEnv),
                    new XAttribute(XNamespace.Xmlns + "siat", NsSiat),
                    new XElement(XName.Get("Header", NsSoapEnv)),
                    new XElement(XName.Get("Body", NsSoapEnv),
                        bodyContent
                    )
                )
            );

            var xmlString = envelope.ToString(SaveOptions.DisableFormatting);
            var url = $"{_opts.UrlBase}/{servicio}";

            _logger.LogInformation("SIAT → {Url}", url);
            _logger.LogDebug("SIAT request XML:\n{Xml}", envelope.ToString());

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _opts.ApiKey);
            request.Content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

            using var response = await _http.SendAsync(request, ct);
            var responseXml = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("SIAT ← HTTP {Status}", (int)response.StatusCode);
            _logger.LogDebug("SIAT response XML:\n{Xml}", responseXml);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"SIAT respondió {(int)response.StatusCode}: {responseXml}");
            }

            var xml = XDocument.Parse(responseXml);
            var fault = BuscarElemento(xml, "Fault");
            if (fault is not null)
            {
                var faultString = fault.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value
                    ?? fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value
                    ?? responseXml;
                _logger.LogWarning("SIAT SOAP Fault: {Fault}", faultString);
                throw new InvalidOperationException($"SIAT SOAP Fault: {faultString}");
            }

            return xml;
        }

        private static XElement Solicitud(string nombre, params XElement[] campos) =>
            new(None + nombre, campos);

        private static XElement Campo(string nombre, object valor) =>
            new(None + nombre, valor);

        // ─────────────────────────────────────────────
        // Helpers de parseo (ignoran namespace — SIAT usa ns2: en respuestas)
        // ─────────────────────────────────────────────
        private static XElement? BuscarElemento(XContainer root, string localName) =>
            root.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

        private static IEnumerable<XElement> BuscarElementos(XElement? root, string localName) =>
            root?.Descendants().Where(e => e.Name.LocalName == localName)
            ?? Enumerable.Empty<XElement>();

        private static DateTime? ParseFecha(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return DateTime.TryParse(valor, out var dt) ? dt : null;
        }

        private static string? ValorElemento(XElement? el, params string[] nombres)
        {
            if (el is null) return null;

            foreach (var nombre in nombres)
            {
                var nodo = el.Elements().FirstOrDefault(e => e.Name.LocalName == nombre)
                    ?? el.Descendants().FirstOrDefault(e => e.Name.LocalName == nombre);

                if (!string.IsNullOrWhiteSpace(nodo?.Value))
                    return nodo.Value;
            }

            return null;
        }

        private static bool ParseTransaccion(XElement? el)
        {
            var valor = el?.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "transaccion")
                ?.Value;
            return valor?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                || valor == "1";
        }

        private static List<CodigoRespuesta> ParseCodigos(XElement? el)
        {
            if (el is null) return new();

            var contenedores = BuscarElementos(el, "codigosRespuesta")
                .Concat(BuscarElementos(el, "CodigosRespuesta"))
                .Concat(BuscarElementos(el, "mensajes"))
                .Concat(BuscarElementos(el, "mensajesList"));

            return contenedores.Select(x => new CodigoRespuesta
            {
                Codigo = int.TryParse(
                    x.Elements().FirstOrDefault(e => e.Name.LocalName == "codigo")?.Value,
                    out var c) ? c : 0,
                Descripcion = x.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "descripcion")?.Value ?? string.Empty
            }).ToList();
        }
    }
}
