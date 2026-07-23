using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KafeYana.Infrastructure.SiatClient
{
    /// <summary>
    /// Cliente HTTP para los servicios SOAP del SIAT.
    /// Usa HttpClient directo porque el SIAT requiere el header "apikey"
    /// que WCF/BasicHttpBinding no maneja limpiamente.
    ///
    /// Instrumentación de contingencia:
    /// Cada llamada SOAP notifica al <see cref="ISiatConnectivityMonitor"/>
    /// con el nombre de la operación y el (suc, pv). El monitor usa eso
    /// para detectar caídas del SIAT y auto-registrar contingencias.
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class SiatHttpClient
    {
        private readonly HttpClient _http;
        private readonly SiatOptions _opts;
        private readonly ILogger<SiatHttpClient> _logger;
        private readonly ISiatConnectivityMonitor _monitor;
        private readonly KafeYana.Infrastructure.Servicios.Facturacion.Utilidades.IContingenciaDebugLogService _debug;

        // Namespaces SOAP del SIAT (confirmados desde Postman)
        private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NsSiat = "https://siat.impuestos.gob.bo/";
        private static readonly XNamespace SiatNs = NsSiat;
        // WSDL elementFormDefault="unqualified" → hijos sin namespace
        private static readonly XNamespace None = XNamespace.None;

        public SiatHttpClient(
            HttpClient http,
            IOptions<SiatOptions> opts,
            ILogger<SiatHttpClient> logger,
            ISiatConnectivityMonitor monitor,
            KafeYana.Infrastructure.Servicios.Facturacion.Utilidades.IContingenciaDebugLogService debug)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;
            _monitor = monitor;
            _debug = debug;
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

            var xml = await EnviarSoapAsync(
                "FacturacionCodigos", body, SiatOperacion.Cuis,
                codigoSucursal, codigoPuntoVenta, ct);

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
            CancellationToken ct = default,
            bool bypassCortocircuito = false)
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

            var xml = await EnviarSoapAsync(
                "FacturacionCodigos", body, SiatOperacion.Cufd,
                codigoSucursal, codigoPuntoVenta, ct, bypassCortocircuito);

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
        // Registro de Evento Significativo (FacturacionCodigos)
        // Notifica al SIN un corte de servicio (sin internet, sin luz, etc.)
        // para que las facturas emitidas durante el mismo se puedan emitir
        // con codigoEmision=4 (Contingencia) citando el codigoRecepcion que
        // devuelve esta operación.
        //
        // WSDL: <registroEventoSignificativo> dentro del servicio
        // FacturacionCodigos. Body: <SolicitudEventoSignificativo> con 13 campos.
        // Response: <registroEventoSignificativoResponse> envolviendo
        // <RespuestaListaEventos> con codigoRecepcionEventoSignificativo +
        // transaccion + listaCodigos (errores).
        //
        // IMPORTANTE: tanto <cufd> como <cufdEvento> son obligatorios. En
        // operación normal ambos son iguales (el CUFD vigente al momento del
        // registro); el SIAT los trata como campos independientes para soportar
        // escenarios donde la contingencia cubre varios días con CUFDs distintos.
        //
        // Ver [[kafeyana-contingencia-siat]] — flujo completo de contingencia.
        // ─────────────────────────────────────────────
        public async Task<RespuestaRegistroEventoSignificativoSiatDto> RegistroEventoSignificativoAsync(
            SolicitudRegistroEventoSignificativoSiatDto solicitud,
            CancellationToken ct = default,
            bool bypassCortocircuito = false)
        {
            var body = new XElement(SiatNs + "registroEventoSignificativo",
                Solicitud("SolicitudEventoSignificativo",
                    // ── ORDEN CRÍTICO — declarado en el XSD del SIAT
                    //    (https://pilotosiatservicios.impuestos.gob.bo/v2/FacturacionOperaciones?wsdl=ServicioFacturacionOperaciones.wsdl)
                    //    <xs:sequence> de SolicitudEventoSignificativo. NO reordenar
                    //    sin volver a verificar el WSDL: el SIAT valida el orden
                    //    exacto y rechaza con errores crípticos si se viola.
                    //    Históricamente el código tenía codigoMotivoEvento al final
                    //    y fechaHoraInicioEvento antes que fechaHoraFinEvento — eso
                    //    hacía que TODAS las contingencias fueran Rechazadas desde
                    //    el origen del proyecto (Gap 8.2: orden XML).
                    Campo("codigoAmbiente", solicitud.CodigoAmbiente),
                    Campo("codigoMotivoEvento", solicitud.CodigoMotivoEvento),
                    Campo("codigoPuntoVenta", solicitud.CodigoPuntoVenta),
                    Campo("codigoSistema", solicitud.CodigoSistema),
                    Campo("codigoSucursal", solicitud.CodigoSucursal),
                    Campo("cufd", solicitud.Cufd),
                    Campo("cufdEvento", solicitud.CufdEvento),
                    Campo("cuis", solicitud.Cuis),
                    Campo("descripcion", solicitud.Descripcion),
                    Campo("fechaHoraFinEvento",
                        SiatFechaEmision.Formatear(solicitud.FechaHoraFinEvento)),
                    Campo("fechaHoraInicioEvento",
                        SiatFechaEmision.Formatear(solicitud.FechaHoraInicioEvento)),
                    Campo("nit", solicitud.Nit)
                )
            );

            // registroEventoSignificativo pertenece al servicio FacturacionOperaciones
            // (NO FacturacionCodigos — ese sólo expone cuis/cufd/verificarNit/
            // verificarComunicacion/cufdMasivo/cuisMasivo/notificaCertificadoRevocado).
            // Mapeo confirmado contra el WSDL del piloto v2.
            var xml = await EnviarSoapAsync(
                "FacturacionOperaciones", body, SiatOperacion.RegistroEventoSignificativo,
                solicitud.CodigoSucursal, solicitud.CodigoPuntoVenta, ct, bypassCortocircuito);

            // El SIAT responde <registroEventoSignificativoResponse> envolviendo
            // <RespuestaListaEventos>. Fallback al nombre genérico por si cambia.
            var respEl = BuscarElemento(xml, "RespuestaListaEventos")
                ?? BuscarElemento(xml, "registroEventoSignificativoResponse");

            return new RespuestaRegistroEventoSignificativoSiatDto
            {
                Transaccion = ParseTransaccion(respEl),
                CodigoRecepcionEventoSignificativo = ValorElemento(
                    respEl, "codigoRecepcionEventoSignificativo"),
                CodigoDescripcion = ValorElemento(respEl, "codigoDescripcion"),
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
            CancellationToken ct = default,
            bool bypassCortocircuito = false)
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

            var xml = await EnviarSoapAsync(
                "FacturacionCodigos", body,
                SiatOperacion.Otros,
                codigoSucursal, _opts.CodigoPuntoVenta,
                ct, bypassCortocircuito);

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

            var xml = await EnviarSoapAsync(
                "FacturacionSincronizacion", body, SiatOperacion.FechaHora,
                codigoSucursal, codigoPuntoVenta, ct);

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
        // Sincronizar Lista de Productos/Servicios (FacturacionSincronizacion)
        // Devuelve el catálogo oficial de productos/servicios del SIN por
        // actividad económica. KafeYana filtra por la actividad principal
        // ANTES de persistir (ver SincronizadorCodigosSiat).
        //
        // La estructura de la respuesta trae <transaccion> y un wrapper con
        // elementos <listaCodigos> hermanos. Cada uno lleva codigoActividad +
        // codigoProducto + descripcionProducto + N <nandina> (códigos
        // aduaneros hermanos que IGNORAMOS porque la tabla CodigosSiat no
        // tiene esa columna).
        //
        //   <RespuestaListaProductos>
        //     <transaccion>true</transaccion>
        //     <listaCodigos>
        //       <codigoActividad>4630600</codigoActividad>
        //       <codigoProducto>1003069</codigoProducto>
        //       <descripcionProducto>café tostado, ...</descripcionProducto>
        //       <nandina>0901.11.90.00</nandina>
        //       <nandina>0901.12.00.00</nandina>
        //       ...
        //     </listaCodigos>
        //     ...
        //   </RespuestaListaProductos>
        // ─────────────────────────────────────────────
        public async Task<SincronizarProductosServiciosResponse> SincronizarListaProductosServiciosAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarListaProductosServicios",
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

            // El wrapper exacto del WSDL es "RespuestaListaProductos"; fallback
            // al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaProductos")
                ?? BuscarElemento(xml, "sincronizarListaProductosServiciosResponse");

            var respuesta = new SincronizarProductosServiciosResponse
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
                    var codigoActividad = ValorElemento(item, "codigoActividad");
                    var codigoProducto = ValorElemento(item, "codigoProducto");
                    var descripcion = ValorElemento(item, "descripcionProducto");

                    if (string.IsNullOrWhiteSpace(codigoActividad)) continue;
                    if (string.IsNullOrWhiteSpace(codigoProducto)) continue;
                    if (string.IsNullOrWhiteSpace(descripcion)) continue;

                    // Ignoramos los <nandina> hijos a propósito: la tabla
                    // CodigosSiat no tiene esa columna. Si se quiere agregar
                    // en el futuro, es ALTER TABLE + List<string> en el DTO.
                    respuesta.ProductosServicios.Add(new ProductoServicioSiatDto
                    {
                        CodigoActividad = codigoActividad.Trim(),
                        CodigoProducto = codigoProducto.Trim(),
                        DescripcionProducto = descripcion.Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica de Eventos Significativos
        // (FacturacionSincronizacion)
        //
        // Devuelve el catálogo oficial de los 7 eventos significativos
        // reconocidos por el SIN. NO se filtra por actividad económica —
        // son universales (a diferencia de productos/servicios o leyendas).
        //
        //   <sincronizarParametricaEventosSignificativosResponse>
        //     <RespuestaListaParametricas>      ← MISMO wrapper que CatDocumentoSector y CatMotivoAnulacion
        //       <transaccion>true</transaccion>
        //       <listaCodigos>                  ← MISMOS hijos que las otras paramétricas
        //         <codigoClasificador>7</codigoClasificador>
        //         <descripcion>CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA</descripcion>
        //       </listaCodigos>
        //       ...
        //     </RespuestaListaParametricas>
        //   </sincronizarParametricaEventosSignificativosResponse>
        //
        // Lista oficial vigente (jun-2026, devuelta por el SIAT):
        //   1 = CORTE DEL SERVICIO DE INTERNET
        //   2 = INACCESIBILIDAD AL SERVICIO WEB DE LA ADMINISTRACIÓN TRIBUTARIA
        //   3 = INGRESO A ZONAS SIN INTERNET POR DESPLIEGUE DE PUNTO DE VENTA
        //   4 = VENTA EN LUGARES SIN INTERNET
        //   5 = VIRUS INFORMÁTICO O FALLA DE SOFTWARE
        //   6 = CAMBIO DE INFRAESTRUCTURA DE SISTEMA O FALLA DE HARDWARE
        //   7 = CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA
        // ─────────────────────────────────────────────
        public async Task<SincronizarEventosSignificativosResponse> SincronizarParametricaEventosSignificativosAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaEventosSignificativos",
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

            // Wrapper exacto (confirmado vía Postman): "RespuestaListaParametricas"
            // — el mismo que usan CatDocumentoSector y CatMotivoAnulacion.
            // Fallback al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaEventosSignificativosResponse");

            var respuesta = new SincronizarEventosSignificativosResponse
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
                // Hijos: <listaCodigos> con <codigoClasificador> + <descripcion>
                // — mismo shape que CatDocumentoSector y CatMotivoAnulacion.
                foreach (var item in respEl.Elements()
                    .Where(e => e.Name.LocalName == "listaCodigos"))
                {
                    var codigoStr = ValorElemento(item, "codigoClasificador", "codigo");
                    var descripcion = ValorElemento(item, "descripcion", "descripcionEvento");
                    if (!int.TryParse(codigoStr, out var codigo)) continue;
                    if (string.IsNullOrWhiteSpace(descripcion)) continue;

                    respuesta.EventosSignificativos.Add(new EventoSignificativoSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = descripcion.Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica de Países de Origen
        // (FacturacionSincronizacion)
        //
        // Devuelve el catálogo oficial de ~211 países reconocidos por el SIN.
        // NO se filtra por actividad económica — es universal.
        //
        //   <sincronizarParametricaPaisOrigenResponse>
        //     <RespuestaListaParametricas>      ← Mismo wrapper que CatDocumentoSector,
        //                                            CatMotivoAnulacion, CatEventoSignificativo
        //       <transaccion>true</transaccion>
        //       <listaCodigos>
        //         <codigoClasificador>22</codigoClasificador>
        //         <descripcion>BOLIVIA (ESTADO PLURINACIONAL DE)</descripcion>
        //       </listaCodigos>
        //       ...
        //     </RespuestaListaParametricas>
        //   </sincronizarParametricaPaisOrigenResponse>
        // ─────────────────────────────────────────────
        public async Task<SincronizarPaisOrigenResponse> SincronizarParametricaPaisOrigenAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaPaisOrigen",
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

            // Wrapper exacto (confirmado vía Postman): "RespuestaListaParametricas"
            // — el mismo que usan CatDocumentoSector, CatMotivoAnulacion y
            // CatEventoSignificativo. Fallback al nombre genérico por si el SIN
            // cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaPaisOrigenResponse");

            var respuesta = new SincronizarPaisOrigenResponse
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
                    var codigoStr = ValorElemento(item, "codigoClasificador", "codigo");
                    var descripcion = ValorElemento(item, "descripcion", "descripcionPais");
                    if (!int.TryParse(codigoStr, out var codigo)) continue;
                    if (string.IsNullOrWhiteSpace(descripcion)) continue;

                    respuesta.PaisesOrigen.Add(new PaisOrigenSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = descripcion.Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica de Tipos de Documento de Identidad
        // Devuelve el catálogo paramétrico de tipos de documento de identidad
        // que el SIN publica. KafeYana usa este catálogo para validar
        // `codigoTipoDocumentoIdentidad` en cada venta facturada.
        //
        // Catálogo actual (verificado contra SIAT piloto, jun-2026):
        //   1 = CI  - CEDULA DE IDENTIDAD
        //   2 = CEX - CEDULA DE IDENTIDAD DE EXTRANJERO
        //   3 = PAS - PASAPORTE
        //   4 = OD  - OTRO DOCUMENTO DE IDENTIDAD
        //   5 = NIT - NÚMERO DE IDENTIFICACIÓN TRIBUTARIA
        //
        // Es catálogo universal (no se filtra por actividad económica).
        // Misma estructura XML que CatDocumentosSector / CatMotivoAnulacion /
        // CatEventoSignificativo / CatPaisOrigen:
        //   <RespuestaListaParametricas>
        //     <transaccion>true</transaccion>
        //     <listaCodigos>
        //       <codigoClasificador>1</codigoClasificador>
        //       <descripcion>CI - CEDULA DE IDENTIDAD</descripcion>
        //     </listaCodigos>
        //     ...
        // ─────────────────────────────────────────────
        public async Task<SincronizarTipoDocumentoIdentidadResponse> SincronizarParametricaTipoDocumentoIdentidadAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaTipoDocumentoIdentidad",
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

            // Wrapper exacto (confirmado vía Postman): "RespuestaListaParametricas"
            // — el mismo que usan CatDocumentosSector, CatMotivoAnulacion,
            // CatEventoSignificativo y CatPaisOrigen. Fallback al nombre
            // genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaTipoDocumentoIdentidadResponse");

            var respuesta = new SincronizarTipoDocumentoIdentidadResponse
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

                    respuesta.TiposDocumentoIdentidad.Add(new TipoDocumentoIdentidadSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Sincronizar Paramétrica de Tipos de Emisión
        // Devuelve el catálogo oficial de los tipos de emisión reconocidos
        // por el SIN. NO se filtra por actividad económica — es universal.
        //
        //   <sincronizarParametricaTipoEmisionResponse>
        //     <RespuestaListaParametricas>      ← Mismo wrapper que las otras
        //                                            paramétricas universales
        //                                            (CatMotivoAnulacion,
        //                                             CatEventoSignificativo,
        //                                             CatPaisOrigen,
        //                                             CatTipoDocumentoIdentidad)
        //       <transaccion>true</transaccion>
        //       <listaCodigos>
        //         <codigoClasificador>1</codigoClasificador>
        //         <descripcion>EN LINEA</descripcion>
        //       </listaCodigos>
        //       ...
        //     </RespuestaListaParametricas>
        //   </sincronizarParametricaTipoEmisionResponse>
        //
        // Lista oficial vigente (jun-2026, devuelta por el SIN):
        //   1 = EN LINEA
        //   2 = FUERA DE LINEA
        //   3 = MASIVO
        //   4 = CONTINGENCIA
        // ─────────────────────────────────────────────
        public async Task<SincronizarTipoEmisionResponse> SincronizarParametricaTipoEmisionAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaTipoEmision",
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

            // Wrapper exacto (confirmado vía Postman): "RespuestaListaParametricas"
            // — el mismo que usan las otras paramétricas universales. Fallback
            // al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaTipoEmisionResponse");

            var respuesta = new SincronizarTipoEmisionResponse
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

                    respuesta.TiposEmision.Add(new TipoEmisionSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        /// <summary>
        /// Sincroniza el catálogo paramétrico de tipos de método de pago contra
        /// el SIAT (<c>sincronizarParametricaTipoMetodoPago</c>).
        ///
        /// Catálogo UNIVERSAL: el SIN devuelve ~308 códigos (métodos simples
        /// + combinaciones de 2 a 4 métodos). Wrapper y shape idéntico a
        /// <c>sincronizarParametricaTipoEmision</c>:
        /// <c>RespuestaListaParametricas</c> + <c>listaCodigos</c>.
        ///
        /// A diferencia de los otros sync, este NO corre diario (ver §15 de
        /// <c>SIAT-SINCRONIZACIONES.md</c>). Solo se invoca al boot del server
        /// y bajo demanda manual vía <c>POST /api/catalogos/sincronizar-metodos-pago</c>.
        /// </summary>
        public async Task<SincronizarTipoMetodoPagoResponse> SincronizarParametricaTipoMetodoPagoAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaTipoMetodoPago",
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

            // Wrapper exacto (mismo que las otras paramétricas universales).
            // Fallback al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaTipoMetodoPagoResponse");

            var respuesta = new SincronizarTipoMetodoPagoResponse
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

                    respuesta.MetodosPago.Add(new TipoMetodoPagoSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        /// <summary>
        /// Sincroniza el catálogo paramétrico de unidades de medida contra
        /// el SIAT (<c>sincronizarParametricaUnidadMedida</c>).
        ///
        /// Catálogo UNIVERSAL: el SIN devuelve ~50–100 códigos (UNIDAD, VASO,
        /// BOTELLA, CAJA, LITRO, MILILITRO, etc.). Wrapper y shape idéntico a
        /// <c>sincronizarParametricaTipoEmision</c> y
        /// <c>sincronizarParametricaTipoMetodoPago</c>:
        /// <c>RespuestaListaParametricas</c> + <c>listaCodigos</c>.
        ///
        /// Corre diario a las 08:10 BOT (sync 12) vía
        /// <c>SincronizacionUnidadMedidaHostedService</c> y bajo demanda manual
        /// vía <c>POST /api/catalogos/sincronizar-unidades-medida</c>.
        /// </summary>
        public async Task<SincronizarParametricaUnidadMedidaResponse> SincronizarParametricaUnidadMedidaAsync(
            string cuis,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var body = new XElement(SiatNs + "sincronizarParametricaUnidadMedida",
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

            // Wrapper exacto (mismo que las otras paramétricas universales).
            // Fallback al nombre genérico por si el SIN cambia el shape.
            var respEl = BuscarElemento(xml, "RespuestaListaParametricas")
                ?? BuscarElemento(xml, "sincronizarParametricaUnidadMedidaResponse");

            var respuesta = new SincronizarParametricaUnidadMedidaResponse
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

                    respuesta.Unidades.Add(new UnidadMedidaSiatDto
                    {
                        Codigo = codigo,
                        Descripcion = (ValorElemento(item, "descripcion") ?? string.Empty).Trim()
                    });
                }
            }

            return respuesta;
        }

        // ─────────────────────────────────────────────
        // Recepción Factura
        //
        // IMPORTANTE: recepcionFactura (singular) NO acepta
        // `codigoRecepcionEventoSignificativo` — ese campo sólo existe en
        // `recepcionPaqueteFactura` (operación masiva) según el WSDL del
        // ServicioFacturacion.
        //
        // Historicamente acá se metía el campo cuando CodigoEmision=2
        // (Contingencia), pero el SIAT respondía:
        //   "Unmarshalling Error: unexpected element codigoRecepcionEventoSignificativo"
        // rompiendo toda la facturación contingencia.
        //
        // Para facturas contingencia el camino correcto es `recepcionPaqueteFactura`
        // (ver RecepcionPaqueteFacturaAsync más abajo). Esta operación singular queda
        // sólo para facturas online (CodigoEmision=1).
        // Ver [[kafeyana-contingencia-paquete-siat]].
        // ─────────────────────────────────────────────
        public async Task<RespuestaRecepcionFacturaDto> RecepcionFacturaAsync(
            SolicitudRecepcionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var camposBase = new List<XElement>
            {
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
            };

            var body = new XElement(SiatNs + "recepcionFactura",
                Solicitud("SolicitudServicioRecepcionFactura", camposBase.ToArray())
            );

            var xml = await EnviarSoapAsync(
                _opts.ServicioRecepcionFactura, body, SiatOperacion.RecepcionFactura,
                solicitud.CodigoSucursal, solicitud.CodigoPuntoVenta, ct);

            return ParsearRespuestaRecepcionFactura(xml);
        }

        /// <summary>
        /// Recepción de un PAQUETE de N facturas contingencia — operación SOAP
        /// <c>recepcionPaqueteFactura</c> del ServicioFacturacionCompraVenta.
        ///
        /// WSDL: el wrapper del request es <c>SolicitudServicioRecepcionPaquete</c>,
        /// que extiende <c>solicitudRecepcionFactura</c> con 3 campos extra:
        /// <c>cafc</c> (opcional, <c>minOccurs="0"</c>), <c>cantidadFacturas</c> (int)
        /// y <c>codigoEvento</c> (long).
        ///
        /// <c>codigoEvento</c> según XSD es <c>xs:long</c> — es el
        /// <c>CodigoRecepcionEventoSignificativo</c> que devolvió el SIAT al
        /// registrar el evento significativo (NO el CodigoMotivo 1-7). Confirmado
        /// por el operador el 28-jun-2026 contra el WSDL de producción.
        ///
        /// El campo <c>archivo</c> se mantiene como <c>xs:base64Binary</c> (mismo tipo
        /// que <c>recepcionFactura</c>). La convención interna (concatenación de los N
        /// XMLs gzip-comprimidos vs otra) la define el XSD externo
        /// <c>solicitudRecepcionPaquete.xsd</c> del SIN; se valida con Postman contra
        /// el piloto antes de producción.
        ///
        /// La respuesta tiene la MISMA estructura que <c>recepcionFacturaResponse</c>:
        /// <c>respuestaRecepcion</c> global con <c>transaccion</c>, <c>codigoRecepcion</c>
        /// único por paquete y <c>mensajesList</c> (cada uno con <c>numeroArchivo</c>,
        /// <c>numeroDetalle</c>, <c>advertencia</c>). El codigoRecepcion se asigna a
        /// todas las ventas del paquete (defensivo hasta que el SIN confirme el shape
        /// por factura).
        ///
        /// Ver [[kafeyana-contingencia-paquete-siat]].
        /// </summary>
        public async Task<RespuestaRecepcionPaqueteFacturaDto> RecepcionPaqueteFacturaAsync(
            SolicitudRecepcionPaqueteFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var camposBase = new List<XElement>
            {
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
            };

            // `cafc` es opcional (minOccurs="0"). Sólo lo incluimos si tiene valor Y
            // el CodigoMotivo del evento es 5, 6 o 7 (factura manual con talonario físico).
            // Para motivos 1-4 (sistema/caída), el SIN espera que el campo no esté presente.
            // Validado contra el piloto: enviar Cafc en motivos 1-4 produce observación 904;
            // no enviar Cafc en motivos 5-7 produce rechazo 902.
            if (solicitud.CodigoMotivo is >= 5 and <= 7
                && !string.IsNullOrWhiteSpace(solicitud.Cafc))
            {
                camposBase.Add(Campo("cafc", solicitud.Cafc));
            }

            camposBase.Add(Campo("cantidadFacturas", solicitud.CantidadFacturas));
            camposBase.Add(Campo("codigoEvento", solicitud.CodigoEvento));

            var body = new XElement(SiatNs + "recepcionPaqueteFactura",
                Solicitud("SolicitudServicioRecepcionPaquete", camposBase.ToArray())
            );

            var xml = await EnviarSoapAsync(
                _opts.ServicioRecepcionPaqueteFactura, body, SiatOperacion.RecepcionPaqueteFactura,
                solicitud.CodigoSucursal, solicitud.CodigoPuntoVenta, ct);

            // Reutilizamos el parser de respuestaRecepcion — la estructura es idéntica.
            var baseRespuesta = ParsearRespuestaRecepcionFactura(xml);

            return new RespuestaRecepcionPaqueteFacturaDto
            {
                Transaccion = baseRespuesta.Transaccion,
                CodigoEstado = baseRespuesta.CodigoEstado,
                CodigoRecepcion = baseRespuesta.CodigoRecepcion,
                CodigoDescripcion = baseRespuesta.CodigoDescripcion,
                CodigosRespuesta = baseRespuesta.CodigosRespuesta,
                // FIX #1: mensajesList viajaba descartado — el parser no lo extraía.
                // Ahora se propaga para que MapearRespuestaPaquete aplique rechazos
                // granulares por numeroArchivo. Ver [[kafeyana-contingencia-siat]].
                MensajesList = baseRespuesta.MensajesList
            };
        }

        /// <summary>
        /// Helper privado: parsea la respuesta SOAP de <c>respuestaRecepcion</c>
        /// (estructura común a <c>recepcionFactura</c>, <c>recepcionPaqueteFactura</c>,
        /// <c>recepcionMasivaFactura</c>). Localiza <c>RespuestaRecepcion</c> o
        /// <c>{operacion}Response</c> y mapea los campos canónicos.
        ///
        /// FIX #1: además de los 5 campos base, ahora extrae <c>mensajesList</c> con
        /// sus <c>numeroArchivo</c>/<c>numeroDetalle</c>/<c>codigo</c>/<c>descripcion</c>/
        /// <c>advertencia</c>. Esta información antes se descartaba — el parser era
        /// "defensivo hasta que el SIN confirme el shape por factura", pero la doc
        /// oficial y la práctica en jun-2026 confirman el shape. Ahora el caller puede
        /// mapear rechazos granulares.
        /// </summary>
        private RespuestaRecepcionFacturaDto ParsearRespuestaRecepcionFactura(XDocument xml)
        {
            var respEl = BuscarElemento(xml, "RespuestaRecepcion")
                ?? BuscarElemento(xml, "recepcionFacturaResponse")
                ?? BuscarElemento(xml, "recepcionPaqueteFacturaResponse")
                ?? BuscarElemento(xml, "recepcionMasivaFacturaResponse");

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
                }).ToList(),
                MensajesList = ParsearMensajesRecepcionPaquete(respEl)
            };
        }

        /// <summary>
        /// FIX #1 — extrae <c>mensajesList</c> de la respuesta de <c>recepcionPaqueteFactura</c>.
        /// Cada entrada referencia un <c>numeroArchivo</c> (1..N del TAR) y opcionalmente
        /// un <c>numeroDetalle</c> (ítem de la factura). El parser es tolerante: campos
        /// faltantes quedan null (no rompe si el SIN no envía todos).
        /// </summary>
        private static List<MensajeRecepcionPaqueteDto> ParsearMensajesRecepcionPaquete(XElement? respEl)
        {
            if (respEl is null) return new List<MensajeRecepcionPaqueteDto>();

            return respEl.Descendants("mensajesList")
                .Select(m => new MensajeRecepcionPaqueteDto
                {
                    NumeroArchivo = m.Element("numeroArchivo")?.Value,
                    NumeroDetalle = m.Element("numeroDetalle")?.Value,
                    Codigo = m.Element("codigo")?.Value,
                    Descripcion = m.Element("descripcion")?.Value,
                    Advertencia = m.Element("advertencia")?.Value,
                })
                .ToList();
        }

        /// <summary>
        /// FIX #1 — operación SOAP <c>validacionRecepcionPaqueteFactura</c>. Tras enviar
        /// un paquete contingencia con <see cref="RecepcionPaqueteFacturaAsync"/>, el SIN
        /// procesa el paquete de forma asíncrona. Esta operación consulta el estado real:
        /// 901 (pendiente), 904 (observada) o 908 (validada). El backend debe esperar
        /// 908 antes de marcar las ventas como Validadas — antes las marcaba apenas el
        /// SOAP síncrono respondía transaccion=true (estado provisional), produciendo
        /// falsos positivos donde el SIN rechazaba el paquete al procesarlo.
        ///
        /// Ver documentacion-contingencia.md líneas 26-28.
        /// </summary>
        public async Task<RespuestaValidacionRecepcionPaqueteDto> ValidacionRecepcionPaqueteFacturaAsync(
            ValidacionRecepcionPaqueteDto solicitud,
            CancellationToken ct = default)
        {
            // FIX #10 (jun-2026): el piloto SIAT rechaza con HTTP 500
            // ("Unmarshalling Error: unexpected element (uri:'', local:'codigoModulo')")
            // cuando el sobre incluye codigoModulo o token. El faultstring del SIAT
            // lista los 12 elementos válidos en el orden del XSD:
            //   cuis, codigoAmbiente, codigoPuntoVenta, codigoEmision, tipoFacturaDocumento,
            //   codigoSistema, nit, codigoSucursal, codigoDocumentoSector, cufd,
            //   codigoRecepcion, codigoModalidad.
            //
            // Antes el backend enviaba 9 elementos en orden distinto y 2 unexpected
            // (codigoModulo, token) — el piloto respondía 500 y la cascada de polling
            // quedaba muerta, marcando la contingencia como caída y registrando un
            // nuevo evento 981 con "RANGO DE FECHAS DE EVENTO SIGNIFICATIVO INVALIDO".
            //
            // Aquí emitimos EXACTAMENTE esos 12 elementos en el orden del XSD.
            var body = new XElement(SiatNs + "validacionRecepcionPaqueteFactura",
                Solicitud("SolicitudServicioValidacionRecepcionPaquete",
                    Campo("cuis", solicitud.Cuis),
                    Campo("codigoAmbiente", solicitud.CodigoAmbiente),
                    Campo("codigoPuntoVenta", solicitud.CodigoPuntoVenta),
                    Campo("codigoEmision", solicitud.CodigoEmision),
                    Campo("tipoFacturaDocumento", solicitud.TipoFacturaDocumento),
                    Campo("codigoSistema", solicitud.CodigoSistema),
                    Campo("nit", solicitud.Nit),
                    Campo("codigoSucursal", solicitud.CodigoSucursal),
                    Campo("codigoDocumentoSector", solicitud.CodigoDocumentoSector),
                    Campo("cufd", solicitud.Cufd),
                    Campo("codigoRecepcion", solicitud.CodigoRecepcion),
                    Campo("codigoModalidad", solicitud.CodigoModalidad)
                )
            );

            var xml = await EnviarSoapAsync(
                _opts.ServicioRecepcionPaqueteFactura, body, SiatOperacion.ValidacionRecepcionPaqueteFactura,
                solicitud.CodigoSucursal, solicitud.CodigoPuntoVenta, ct);

            var respEl = BuscarElemento(xml, "RespuestaServicioValidacionRecepcionPaquete")
                ?? BuscarElemento(xml, "validacionRecepcionPaqueteFacturaResponse");

            return new RespuestaValidacionRecepcionPaqueteDto
            {
                Transaccion = ParseTransaccion(respEl),
                CodigoEstado = int.TryParse(ValorElemento(respEl, "codigoEstado"), out var estado) ? estado : null,
                CodigoRecepcion = ValorElemento(respEl, "codigoRecepcion"),
                CodigoDescripcion = ValorElemento(respEl, "codigoDescripcion"),
                MensajesList = ParsearMensajesRecepcionPaquete(respEl),
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
        //
        // Soporte contingencia (TipoEmision=2): cuando la nota se emite durante
        // un evento significativo, se agrega el campo
        // "codigoRecepcionEventoSignificativo" al sobre para que el SIN vincule
        // la nota al evento. El SIAT lo cruza con su log: si no coincide, rechaza.
        // Ver [[kafeyana-contingencia-siat]].
        // ─────────────────────────────────────────────
        public async Task<RespuestaRecepcionNotaAjusteDto> RecepcionDocumentoAjusteAsync(
            SolicitudRecepcionNotaAjusteDto solicitud,
            CancellationToken ct = default)
        {
            // Fail-closed: si llega el código de recepción del evento con
            // CodigoEmision != 2, es un bug del flujo offline → lanzar antes
            // de tocar el SIAT para no generar tráfico espurio.
            if (!string.IsNullOrWhiteSpace(solicitud.CodigoRecepcionEventoSignificativo)
                && solicitud.CodigoEmision != 2)
            {
                throw new InvalidOperationException(
                    "codigoRecepcionEventoSignificativo solo puede viajar cuando CodigoEmision = 2 (Contingencia). "
                  + $"Recibido CodigoEmision={solicitud.CodigoEmision}.");
            }

            var camposBase = new List<XElement>
            {
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
            };

            if (solicitud.CodigoEmision == 2
                && !string.IsNullOrWhiteSpace(solicitud.CodigoRecepcionEventoSignificativo))
            {
                camposBase.Add(Campo(
                    "codigoRecepcionEventoSignificativo",
                    solicitud.CodigoRecepcionEventoSignificativo));
            }

            var body = new XElement(SiatNs + "recepcionDocumentoAjuste",
                Solicitud("SolicitudServicioRecepcionDocumentoAjuste", camposBase.ToArray())
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
        // Método interno: arma el envelope y envía (instrumentado con monitor)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Overload usado por operaciones AUXILIARES (sync de catálogos, anulaciones
        /// que no son del path crítico de cobro, etc.). Sus fallos no cuentan para el
        /// umbral de contingencia porque no bloquean al cajero.
        /// </summary>
        private Task<XDocument> EnviarSoapAsync(
            string servicio,
            XElement bodyContent,
            CancellationToken ct)
            => EnviarSoapAsync(
                servicio, bodyContent,
                SiatOperacion.Otros,
                _opts.CodigoSucursal, _opts.CodigoPuntoVenta,
                ct);

        /// <summary>
        /// Método principal de envío SOAP. Notifica al <see cref="ISiatConnectivityMonitor"/>
        /// el éxito o fallo de cada operación, con el nombre canónico de la operación
        /// y el (suc, pv) afectado. Las operaciones críticas (CUIS/CUFD/FechaHora/RecepcionFactura)
        /// son las que cuentan para detectar la caída del SIAT.
        ///
        /// <paramref name="bypassCortocircuito"/> lo usa SOLO
        /// <c>SiatConnectivityProbeService</c> para hacer una llamada real
        /// contra SIAT sin pasar por el cortocircuito del monitor — es
        /// justamente el mecanismo que detecta recuperación de SIAT.
        /// Para todo el resto del código (cobros, sync, etc.) debe ser false.
        /// </summary>
        private async Task<XDocument> EnviarSoapAsync(
            string servicio,
            XElement bodyContent,
            string operacion,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct,
            bool bypassCortocircuito = false)
        {
            // Cortocircuito: si el monitor ya sabe que el SIN está caído,
            // lanzamos SiatOfflineException con (suc, pv) sin tocar la red.
            // El consumidor (FacturaSiatEnvioService) captura esta excepción
            // y deriva a "modo contingencia" con persistencia local.
            // Sin esto, cada cobro durante el corte esperaría el timeout
            // completo del HttpClient antes de fallar — tiempo muerto que
            // el cajero mira en una pantalla congelada.
            //
            // Hay DOS formas de bypass:
            // 1) bypassCortocircuito=true (param explícito) — para casos donde
            //    sabemos que SIAT está vivo y no queremos esperar el flujo
            //    natural de recuperación.
            // 2) _monitor.TieneBypassActivo(suc, pv) — flag global del monitor,
            //    seteado por el probe cuando detecta SIAT alcanzable. Permite
            //    que llamadas posteriores pasen sin tener que propagar el flag
            //    por toda la cadena de llamadas.
            if (!bypassCortocircuito
                && !_monitor.TieneBypassActivo(codigoSucursal, codigoPuntoVenta)
                && !_monitor.EstaEnLinea(codigoSucursal, codigoPuntoVenta))
            {
                throw new SiatOfflineException(
                    "Circuito SIAT abierto. Modo contingencia activo.",
                    codigoSucursal,
                    codigoPuntoVenta);
            }

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

            // Log a archivo contingencia: SOAP request completo (independiente del
            // log level — el operador ve qué se está enviando sin tener que
            // habilitar Debug en producción). Reemplaza el bloque /tmp/ previo
            // (que no funcionaba en Windows production).
            _debug.LogSoapRequest(operacion, envelope.ToString(), url);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("apikey", _opts.ApiKey);
                request.Content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

                using var response = await _http.SendAsync(request, ct);
                var responseXml = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("SIAT ← HTTP {Status}", (int)response.StatusCode);
                _logger.LogDebug("SIAT response XML:\n{Xml}", responseXml);

                // Log a archivo contingencia: SOAP response completo + apikey mask.
                _debug.LogSoapResponse(operacion, (int)response.StatusCode, responseXml, _debug.MaskApikey(_opts.ApiKey));

                if (!response.IsSuccessStatusCode)
                {
                    var httpEx = new HttpRequestException(
                        $"SIAT respondió {(int)response.StatusCode}: {responseXml}");
                    await NotificarFalloAsync(operacion, httpEx, codigoSucursal, codigoPuntoVenta, ct);
                    _debug.LogError("SiatHttpClient", "http_no_success",
                        $"operacion={operacion} url={url} http={(int)response.StatusCode}", httpEx);
                    throw httpEx;
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
                    var faultEx = new InvalidOperationException($"SIAT SOAP Fault: {faultString}");
                    // SOAP Fault NO se cuenta como caída (es un rechazo de negocio,
                    // no un problema de conectividad).
                    await _monitor.ReportarExitoAsync(operacion, codigoSucursal, codigoPuntoVenta, ct);
                    _debug.LogError("SiatHttpClient", "soap_fault",
                        $"operacion={operacion} url={url} faultString={TruncarParaLog(faultString, 2048)}", faultEx);
                    throw faultEx;
                }

                await _monitor.ReportarExitoAsync(operacion, codigoSucursal, codigoPuntoVenta, ct);
                return xml;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelación cooperativa del request, no es caída del SIAT.
                _debug.LogWarn("SiatHttpClient", "cancelled", $"operacion={operacion} url={url}", null);
                throw;
            }
            catch (Exception ex)
            {
                await NotificarFalloAsync(operacion, ex, codigoSucursal, codigoPuntoVenta, CancellationToken.None);
                _debug.LogError("SiatHttpClient", "exception", $"operacion={operacion} url={url}", ex);
                throw;
            }
        }

        private static string TruncarParaLog(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";

        private async Task NotificarFalloAsync(
            string operacion,
            Exception ex,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct)
        {
            try
            {
                await _monitor.ReportarFalloAsync(operacion, ex, codigoSucursal, codigoPuntoVenta, ct);
            }
            catch (Exception notifyEx)
            {
                // No propagamos errores del monitor (defensa en profundidad).
                _logger.LogError(notifyEx,
                    "Error notificando fallo al monitor SIAT. Op={Op}", operacion);
            }
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

        // ─────────────────────────────────────────────
        // Health check barato — usado por SiatConnectivityProbeService
        // ─────────────────────────────────────────────

        /// <summary>
        /// HTTP GET plano al <see cref="SiatOptions.UrlBase"/>. NO pasa por el
        /// cortocircuito del monitor (es justamente el mecanismo que reabre
        /// el circuito cuando SIAT vuelve a estar alcanzable). Solo HTTP 2xx
        /// (éxito) y 3xx (redirect) cuentan como "SIAT alcanzable". HTTP 4xx
        /// y 5xx — especialmente 503 Service Unavailable — cuentan como
        /// caído: SIAT responde pero el servicio no está disponible
        /// (mantenimiento, sobrecarga, backend caído). Timeout, DNS fail y
        /// connection refused también cuentan como caído.
        ///
        /// <para>
        /// <b>Por qué NO contar 5xx como vivo</b>: este método es la
        /// señal que dispara <c>ReenviarRegistroAsync</c> del monitor de
        /// contingencia. Si SIAT responde 503 (degradado pero TCP vivo), un
        /// "vivo" incorrecto lleva a re-registrar un evento significativo
        /// que el SIAT realmente caído rechaza con
        /// <c>[984] EL EVENTO SIGNIFICATIVO NO CORRESPONDE AL CUFD DEL
        /// EVENTO REGISTRADO</c>. Ese rechazo deja el evento en estado
        /// <c>Rechazado</c> con <c>CodigoRecepcionEventoSignificativo = NULL</c>,
        /// y todas las ventas contingencia asociadas quedan varadas
        /// (fail-closed en <c>FacturaSiatEnvioService</c>). Validado contra
        /// piloto SIAT jun-2026 (ver memoria <c>kafeyana-probe-503-clasificacion</c>).
        /// </para>
        ///
        /// <para>
        /// <b>Por qué SÍ contar 4xx como vivo</b>: el endpoint SOAP del
        /// SIAT devuelve 404 o 405 ante un GET a la raíz (es un endpoint
        /// SOAP, no REST). Eso es una respuesta válida del stack HTTP
        /// (TCP conectó, el servidor procesó la request) y solo indica
        /// que ese recurso/método no existe. Si contóramos 4xx como caído,
        /// el probe NUNCA reportaría vivo y la contingencia nunca cerraría
        /// en condiciones normales de operación. Por eso la condición es
        /// <c>StatusCode &lt; 500</c>: cualquier respuesta del servidor
        /// que NO sea error 5xx cuenta como alcanzable.
        /// </para>
        ///
        /// <para>
        /// <b>Por qué se usa el WSDL de un servicio específico</b>: el
        /// GET al <see cref="SiatOptions.UrlBase"/> raíz (<c>/v2</c>)
        /// SIEMPRE devuelve 503 del nginx front, porque <c>/v2</c> no es
        /// un endpoint real — es solo el path base del API. Para probar
        /// liveness real, hacemos GET al WSDL de un servicio SOAP
        /// registrado (<c>ServicioFacturacionCompraVenta?wsdl</c>). El
        /// WSDL devuelve 200 cuando el servicio está vivo y registrado
        /// en el balanceador. Esto fue bug del diseño original
        /// (jun-2026) — el autor asumía que el root devolvería 404/405
        /// pero en realidad devuelve 503. Validado contra piloto SIAT.
        /// </para>
        ///
        /// Usado por <c>SiatConnectivityProbeService</c> para romper el
        /// chicken-and-egg del monitor: si nadie factura o sincroniza, este
        /// ping periódico detecta recuperación sin depender de tráfico de cobros.
        /// </summary>
        public async Task<bool> PingSiatAsync(CancellationToken ct = default)
        {
            try
            {
                // _http.BaseAddress está configurado como _opts.UrlBase
                // (DependencyInjection.cs:58) — ej. "https://piloto.../v2"
                // SIN trailing slash. Si pasamos un relative path SIN leading
                // slash, HttpClient usa Uri resolution rules que REEMPLAZAN
                // el último segmento del BaseAddress en vez de concatenar
                // (bug clásico: BaseAddress="/v2" + relative="Servicio"
                //  → "https://host/Servicio" sin "/v2"). Por eso construimos
                // la URL completa explícitamente vía string interpolation,
                // mismo patrón que EnviarSoapAsync usa en la línea 1706.
                //
                // El WSDL de ServicioFacturacionCompraVenta es el liveness
                // check más estable — está cacheado en el balanceador y
                // devuelve 200 cuando el upstream del servicio está vivo.
                // El path raíz /v2 SIEMPRE devuelve 503 del nginx front
                // porque /v2 no es un endpoint real.
                var url = $"{_opts.UrlBase}/ServicioFacturacionCompraVenta?wsdl";
                using var resp = await _http.GetAsync(url, ct);
                // Cualquier respuesta < 500 cuenta como vivo:
                //   - 2xx/3xx → alcanzable (200 OK del WSDL)
                //   - 4xx     → alcanzable (servidor responde, método/recurso
                //                no existe — pero el stack HTTP funciona)
                //   - 5xx     → CAÍDO (503 Service Unavailable, etc.)
                //   - timeout/DNS/connection refused → CAÍDO (capturado abajo)
                return (int)resp.StatusCode < 500;
            }
            catch
            {
                return false;
            }
        }
    }
}
