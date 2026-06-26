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
        // Sincronizar Actividades (FacturacionSincronizacion)
        // ─────────────────────────────────────────────
        public async Task<SincronizarActividadesResponse> SincronizarActividadesAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            // IMPORTANTE: el nombre del elemento interno es "SolicitudSincronizacion"
            // (no "SolicitudSincronizarActividades"). Verificado por respuesta del SIAT.
            var body = new XElement(SiatNs + "sincronizarActividades",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            // El SIAT responde con <RespuestaListaActividades> envolviendo la lista.
            var respEl = BuscarElemento(xml, "RespuestaListaActividades")
                ?? BuscarElemento(xml, "sincronizarActividadesResponse");

            var respuesta = new SincronizarActividadesResponse
            {
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };

            // Estructura observada en piloto (jun-2026):
            // <RespuestaListaActividades>
            //     <transaccion>true</transaccion>
            //     <listaActividades>
            //         <codigoCaeb>6920000</codigoCaeb>
            //         <descripcion>...</descripcion>
            //         <tipoActividad>S</tipoActividad>
            //     </listaActividades>
            //     <listaActividades>...</listaActividades>
            // </RespuestaListaActividades>
            //
            // OJO: el SIAT NO usa un contenedor único. CADA actividad
            // es un elemento <listaActividades> independiente.
            if (respEl is not null)
            {
                foreach (var act in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaActividades"))
                {
                    var caeb = ValorElemento(act, "codigoCaeb");
                    if (string.IsNullOrWhiteSpace(caeb))
                        continue;

                    respuesta.Actividades.Add(new ActividadSiatDto
                    {
                        CodigoCaeb = caeb,
                        Descripcion = ValorElemento(act, "descripcion") ?? string.Empty,
                        TipoActividad = ValorElemento(act, "tipoActividad") ?? string.Empty
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Fecha Hora (FacturacionSincronizacion)
        // Devuelve la hora oficial del SIN — se usa en fechaEmision y fechaEnvio
        // del XML para evitar rechazo por reloj desincronizado.
        // ─────────────────────────────────────────────
        public async Task<SincronizarFechaHoraResponse> SincronizarFechaHoraAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarFechaHora",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            var respEl = BuscarElemento(xml, "RespuestaFechaHora")
                ?? BuscarElemento(xml, "sincronizarFechaHoraResponse");

            return new SincronizarFechaHoraResponse
            {
                Transaccion = ParseTransaccion(respEl),
                FechaHora = ParseFecha(ValorElemento(respEl, "fechaHora"))
            };
        }

        // ─────────────────────────────────────────────
        // Sincronizar Lista Actividades Documento Sector (FacturacionSincronizacion)
        // Devuelve la MATRIZ Actividad ↔ Documento Sector que el SIN publica:
        // para cada (codigoActividad) enumera los (codigoDocumentoSector) que
        // puede emitir, con su (tipoDocumentoSector) oficial (FCV, NCD,
        // NCDDE, FAC_CVB, …).
        //
        // A diferencia de "sincronizarParametricaTipoDocumentoSector" (catálogo
        // plano de sectores), este endpoint cruza la actividad con su sector
        // para que la UI / el preparer puedan VALIDAR la combinación antes de
        // enviar la factura al SIAT.
        // ─────────────────────────────────────────────
        public async Task<SincronizarActividadesDocumentoSectorResponse> SincronizarActividadesDocumentoSectorAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarListaActividadesDocumentoSector",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            // Estructura observada en piloto (jun-2026):
            // <sincronizarListaActividadesDocumentoSectorResponse>
            //   <RespuestaListaActividadesDocumentoSector>
            //     <transaccion>true</transaccion>
            //     <listaActividadesDocumentoSector>
            //       <codigoActividad>4630600</codigoActividad>
            //       <codigoDocumentoSector>24</codigoDocumentoSector>
            //       <tipoDocumentoSector>NCD</tipoDocumentoSector>
            //     </listaActividadesDocumentoSector>
            //     ...
            //   </RespuestaListaActividadesDocumentoSector>
            // </sincronizarListaActividadesDocumentoSectorResponse>
            var respEl = BuscarElemento(xml, "RespuestaListaActividadesDocumentoSector")
                ?? BuscarElemento(xml, "sincronizarListaActividadesDocumentoSectorResponse");

            var respuesta = new SincronizarActividadesDocumentoSectorResponse
            {
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };

            if (respEl is not null)
            {
                foreach (var item in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaActividadesDocumentoSector"))
                {
                    var codigoActividad = ValorElemento(item, "codigoActividad");
                    if (string.IsNullOrWhiteSpace(codigoActividad)) continue;

                    var codigoSectorStr = ValorElemento(item, "codigoDocumentoSector");
                    if (string.IsNullOrWhiteSpace(codigoSectorStr)) continue;
                    if (!int.TryParse(codigoSectorStr, out var codigoSector)) continue;

                    respuesta.ActividadesDocumentoSector.Add(new ActividadDocumentoSectorSiatDto
                    {
                        CodigoActividad = codigoActividad.Trim(),
                        CodigoDocumentoSector = codigoSector,
                        TipoDocumentoSector = (ValorElemento(item, "tipoDocumentoSector") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica Tipo Documento Sector (FacturacionSincronizacion)
        // Devuelve el catálogo de documentos sectoriales que el SIN acepta
        // (Factura Compra-Venta, Nota Crédito-Débito, etc.). Se usa para llenar
        // <codigoDocumentoSector> en el XML de la factura.
        // ─────────────────────────────────────────────
        public async Task<SincronizarDocumentosSectorResponse> SincronizarDocumentosSectorAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaTipoDocumentoSector",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            // Estructura observada en piloto (jun-2026):
            // <sincronizarParametricaTipoDocumentoSectorResponse>
            //   <RespuestaListaParametricas>
            //     <transaccion>true</transaccion>
            //     <listaCodigos>
            //       <codigoClasificador>1</codigoClasificador>
            //       <descripcion>FACTURA COMPRA-VENTA</descripcion>
            //     </listaCodigos>
            //     <listaCodigos>...</listaCodigos>
            //   </RespuestaListaParametricas>
            // </sincronizarParametricaTipoDocumentoSectorResponse>
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaTipoDocumentoSectorResponse");

            var respuesta = new SincronizarDocumentosSectorResponse
            {
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };

            if (respEl is not null)
            {
                foreach (var item in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaCodigos"))
                {
                    var codigoStr = ValorElemento(item, "codigoClasificador");
                    if (string.IsNullOrWhiteSpace(codigoStr)) continue;
                    if (!int.TryParse(codigoStr, out var codigo)) continue;

                    respuesta.DocumentosSector.Add(new DocumentoSectorSiatDto
                    {
                        CodigoClasificador = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica Motivo Anulación (FacturacionSincronizacion)
        // Devuelve el catálogo paramétrico de motivos de anulación que el SIN
        // publica. Se usa tanto para anular facturas (CompraVenta, sector 1)
        // como para anular notas de crédito/débito (sector 24).
        //
        // Catálogo actual (verificado contra WSDL piloto, jun-2026):
        //   1 = FACTURA MAL EMITIDA
        //   2 = NOTA DE CREDITO-DEBITO MAL EMITIDA
        //   3 = DATOS DE EMISION INCORRECTOS
        //   4 = FACTURA O NOTA DE CREDITO-DEBITO DEVUELTA
        // ─────────────────────────────────────────────
        public async Task<SincronizarMotivoAnulacionResponse> SincronizarParametricaMotivoAnulacionAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaMotivoAnulacion",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            // Misma estructura que SincronizarDocumentosSector:
            //   <RespuestaListaParametricas>
            //     <transaccion>true</transaccion>
            //     <listaCodigos>
            //       <codigoClasificador>1</codigoClasificador>
            //       <descripcion>FACTURA MAL EMITIDA</descripcion>
            //     </listaCodigos>
            //     ...
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaMotivoAnulacionResponse");

            var respuesta = new SincronizarMotivoAnulacionResponse
            {
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };

            if (respEl is not null)
            {
                foreach (var item in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaCodigos"))
                {
                    var codigoStr = ValorElemento(item, "codigoClasificador");
                    if (string.IsNullOrWhiteSpace(codigoStr)) continue;
                    if (!int.TryParse(codigoStr, out var codigo)) continue;

                    respuesta.Motivos.Add(new MotivoAnulacionSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Lista de Leyendas para Factura (FacturacionSincronizacion)
        // Devuelve la lista oficial de leyendas obligatorias que el SIN publica
        // por actividad económica. KafeYana filtra por la actividad principal
        // ANTES de persistir (ver SincronizadorCatLeyenda).
        //
        // La estructura de la respuesta es la misma que las otras paramétricas
        // (lista de hermanos con <transaccion> y un wrapper), pero los elementos
        // se llaman <listaLeyendas> y traen 2 campos: codigoActividad +
        // descripcionLeyenda (en vez del par codigoClasificador + descripcion).
        //
        //   <RespuestaListaParametricasLeyendas>
        //     <transaccion>true</transaccion>
        //     <listaLeyendas>
        //       <codigoActividad>4630600</codigoActividad>
        //       <descripcionLeyenda>Ley N° 453: ...</descripcionLeyenda>
        //     </listaLeyendas>
        //     ...
        //   </RespuestaListaParametricasLeyendas>
        // ─────────────────────────────────────────────
        public async Task<SincronizarLeyendasResponse> SincronizarListaLeyendasFacturaAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarListaLeyendasFactura",
                Solicitud("SolicitudSincronizacion",
                    Campo("codigoAmbiente", _opts.CodigoAmbiente),
                    Campo("codigoPuntoVenta", codigoPuntoVenta),
                    Campo("codigoSistema", _opts.CodigoSistema),
                    Campo("codigoSucursal", codigoSucursal),
                    Campo("cuis", cuis),
                    Campo("nit", _opts.Nit)
                )
            );

            var xml = await EnviarSoapAsync("FacturacionSincronizacion", body, ct);

            // Wrapper distinto al de motivos (sufijo "Leyendas"), pero fallback
            // al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricasLeyendas")
                ?? BuscarElemento(xml, "sincronizarListaLeyendasFacturaResponse");

            var respuesta = new SincronizarLeyendasResponse
            {
                Transaccion = ParseTransaccion(respEl),
                CodigosRespuesta = ParseCodigos(respEl)
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };

            if (respEl is not null)
            {
                foreach (var item in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaLeyendas"))
                {
                    var codigoActividad = ValorElemento(item, "codigoActividad");
                    var descripcion = ValorElemento(item, "descripcionLeyenda");

                    if (string.IsNullOrWhiteSpace(codigoActividad)) continue;
                    if (string.IsNullOrWhiteSpace(descripcion)) continue;

                    respuesta.Leyendas.Add(new LeyendaSiatDto
                    {
                        CodigoActividad = codigoActividad.Trim(),
                        DescripcionLeyenda = descripcion.Trim()
                    });
                }
            }

            return respuesta;
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

        // ─────────────────────────────────────────────
        // Reversión Anulación Factura
        // ─────────────────────────────────────────────
        public async Task<RespuestaReversionAnulacionFacturaDto> ReversionAnulacionFacturaAsync(
            SolicitudReversionAnulacionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "reversionAnulacionFactura",
                Solicitud("SolicitudServicioReversionAnulacionFactura",
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
                    Campo("cuf", solicitud.Cuf)
                )
            );

            var xml = await EnviarSoapAsync(_opts.ServicioReversionAnulacionFactura, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaReversionAnulacion")
                ?? BuscarElemento(xml, "reversionAnulacionFacturaResponse");

            return new RespuestaReversionAnulacionFacturaDto
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

        // ─────────────────────────────────────────────
        // Recepción Nota de Crédito/Débito
        // IMPORTANTE: el sobre NO incluye "cufd" (verificado contra scripts/soap_recepcionDocumentoAjuste.xml).
        // Diferencia intencional con RecepcionFactura.
        // ─────────────────────────────────────────────
        public async Task<RespuestaRecepcionNotaAjusteDto> RecepcionDocumentoAjusteAsync(
            SolicitudRecepcionNotaAjusteDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "recepcionDocumentoAjuste",
                Solicitud("SolicitudServicioRecepcionDocumentoAjuste",
                    Campo("codigoAmbiente", solicitud.CodigoAmbiente),
                    Campo("codigoDocumentoSector", solicitud.CodigoDocumentoSector),
                    Campo("codigoEmision", solicitud.CodigoEmision),
                    Campo("codigoModalidad", solicitud.CodigoModalidad),
                    Campo("codigoPuntoVenta", solicitud.CodigoPuntoVenta),
                    Campo("codigoSistema", solicitud.CodigoSistema),
                    Campo("codigoSucursal", solicitud.CodigoSucursal),
                    Campo("cuis", solicitud.Cuis),
                    Campo("nit", solicitud.Nit),
                    Campo("tipoFacturaDocumento", solicitud.TipoFacturaDocumento),
                    Campo("archivo", solicitud.Archivo),
                    Campo("fechaEnvio", FormatearFechaEnvio(solicitud.FechaEnvio)),
                    Campo("hashArchivo", solicitud.HashArchivo)
                )
            );

            var xml = await EnviarSoapAsync(_opts.ServicioRecepcionNotaAjuste, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaRecepcion")
                ?? BuscarElemento(xml, "recepcionDocumentoAjusteResponse");

            return new RespuestaRecepcionNotaAjusteDto
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
        // Anulación Nota de Crédito/Débito
        // (ServicioFacturacionDocumentoAjuste, sector 24, tipoFactura 3)
        // Espejo de AnulacionFacturaAsync. Orden estricto del WSDL:
        //   codigoAmbiente, codigoDocumentoSector, codigoEmision, codigoModalidad,
        //   codigoPuntoVenta, codigoSistema, codigoSucursal, cufd, cuis, nit,
        //   tipoFacturaDocumento, codigoMotivo, cuf.
        // ─────────────────────────────────────────────
        public async Task<RespuestaAnulacionDocumentoAjusteDto> AnulacionDocumentoAjusteAsync(
            SolicitudAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "anulacionDocumentoAjuste",
                Solicitud("SolicitudServicioAnulacionDocumentoAjuste",
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

            var xml = await EnviarSoapAsync(_opts.ServicioAnulacionNotaAjuste, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaServicioFacturacion")
                ?? BuscarElemento(xml, "anulacionDocumentoAjusteResponse");

            return new RespuestaAnulacionDocumentoAjusteDto
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

        // ─────────────────────────────────────────────
        // Reversión Anulación Nota de Crédito/Débito
        // Misma estructura que AnulacionDocumentoAjusteAsync pero sin codigoMotivo.
        // El SIAT exige este orden estricto:
        //   codigoAmbiente, codigoDocumentoSector, codigoEmision, codigoModalidad,
        //   codigoPuntoVenta, codigoSistema, codigoSucursal, cufd, cuis, nit,
        //   tipoFacturaDocumento, cuf.
        // ─────────────────────────────────────────────
        public async Task<RespuestaReversionAnulacionDocumentoAjusteDto> ReversionAnulacionDocumentoAjusteAsync(
            SolicitudReversionAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "reversionAnulacionDocumentoAjuste",
                Solicitud("SolicitudServicioReversionAnulacionDocumentoAjuste",
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
                    Campo("cuf", solicitud.Cuf)
                )
            );

            var xml = await EnviarSoapAsync(_opts.ServicioReversionAnulacionNotaAjuste, body, ct);

            var respEl = BuscarElemento(xml, "RespuestaServicioFacturacion")
                ?? BuscarElemento(xml, "reversionAnulacionDocumentoAjusteResponse");

            return new RespuestaReversionAnulacionDocumentoAjusteDto
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
            if (!DateTime.TryParse(valor, out var dt)) return null;
            // El SIAT devuelve la hora de Bolivia (America/La_Paz, UTC-4) sin sufijo de zona.
            // La marcamos como Unspecified para que SiatFechaEmision.Formatear la devuelva
            // tal cual (es la hora que se envía al XML y al propio SIAT en sincronizarFechaHora).
            // La conversión a UTC para la BD se hace con ToUtcForDb() en VentaServices.
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
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
