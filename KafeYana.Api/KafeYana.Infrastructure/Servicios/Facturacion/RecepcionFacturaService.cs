using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class RecepcionFacturaService : IRecepcionFacturaService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<RecepcionFacturaService> _logger;
        private readonly KafeYana.Infrastructure.Servicios.Facturacion.Utilidades.IContingenciaDebugLogService _debug;

        public RecepcionFacturaService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<RecepcionFacturaService> logger,
            KafeYana.Infrastructure.Servicios.Facturacion.Utilidades.IContingenciaDebugLogService debug)
        {
            _siat = siat;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
            _debug = debug;
        }

        public string CalcularHashArchivo(string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de factura es requerido para calcular el hash.", nameof(archivo));

            return SiatSha256.GenerarHashArchivo(archivo);
        }

        public async Task<SolicitudRecepcionFacturaDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de factura es requerido.", nameof(archivo));

            // Si el caller no pasa fechaEmision, usamos la hora UTC actual.
            // Lo importante es que el CUFD que se consulte/vuelva a pedir coincida
            // con la fechaEmision usada al generar el CUF (error 1002/1003 si no).
            var fechaEmisionRef = fechaEmision ?? SiatFechaEmision.AhoraUtc();

            // Sucursal/PV efectivos: si el caller los prefijó (caso del cobro), se usan
            // esos. Si no, se cae a appsettings.json como antes.
            var sucEfectiva = codigoSucursal ?? _opts.CodigoSucursal;
            var pvEfectivo = codigoPuntoVenta ?? _opts.CodigoPuntoVenta;

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(sucEfectiva, pvEfectivo, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de facturar.");

            // Si el caller prefijó el CUFD (lo usó para generar el CUF en la misma
            // operación), lo reusamos tal cual para evitar la divergencia entre el
            // CUF embebido en el XML y el CUFD del sobre SOAP (errores 1002/1003).
            // Si no, hacemos la consulta independiente legacy.
            string cufdCodigo;
            if (!string.IsNullOrWhiteSpace(cufdPrefijo))
            {
                cufdCodigo = cufdPrefijo.Trim();
                _logger.LogInformation(
                    "RecepcionFactura usando CUFD prefijado por el caller ({Cufd}) para mantener consistencia con el CUF",
                    cufdCodigo);
            }
            else
            {
                _logger.LogWarning(
                    "RecepcionFactura SIN cufdPrefijo — haciendo fetch independiente (legacy path). "
                    + "Si el CUF fue generado con un CUFD distinto al que se obtenga aquí, "
                    + "el SIAT rechazará con 1002/1003.");

                var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                    sucEfectiva, pvEfectivo, fechaEmisionRef, ct);

                if (!cufd.EsVigente())
                    throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de facturar.");

                cufdCodigo = cufd.Codigo;
            }

            hashArchivo = string.IsNullOrWhiteSpace(hashArchivo)
                ? CalcularHashArchivo(archivo)
                : hashArchivo.Trim();

            var solicitud = new SolicitudRecepcionFacturaDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSector,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = pvEfectivo,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = sucEfectiva,
                Cufd = cufdCodigo,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                Archivo = archivo,
                HashArchivo = hashArchivo,
                FechaEnvio = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Solicitud RecepcionFactura preparada. HashArchivo={Hash}. Suc={Suc}, PV={PV}. CUIS vigente hasta {CuisVigencia}, CUFD={Cufd}",
                hashArchivo, sucEfectiva, pvEfectivo, cuis.FechaVigencia, cufdCodigo);

            return solicitud;
        }

        public async Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            string? cufdPrefijo = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            // FIX GAP 11.f retroactivo: ventas creadas antes del fix en SiatGzip.cs
            // tienen Venta.XmlBase64 con \r\n cada 76 chars (InsertLineBreaks default).
            // Esas ventas fallarían con 920 al reenviarse por el flujo online. Limpiamos
            // el base64 y recalculamos el hash sobre los bytes gzip ANTES de armar el DTO,
            // de modo que tanto ventas nuevas como viejas se envían correctas.
            // Mismo patrón que ArmarArchivoPaquete (contingencia) líneas 379-381.
            (archivo, hashArchivo) = SanitizarArchivoYHash(archivo, hashArchivo);

            var dto = await PrepararSolicitudAsync(
                archivo, hashArchivo, fechaEmision,
                cufdPrefijo, codigoSucursal, codigoPuntoVenta, ct);
            var respuesta = await _siat.RecepcionFacturaAsync(dto, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó RecepcionFactura. Estado={Estado}. Mensajes: {Errores}",
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "RecepcionFactura aceptada. CodigoRecepcion={Codigo}",
                    respuesta.CodigoRecepcion);
            }

            return respuesta;
        }

        /// <summary>
        /// Overload para reenvío de facturas emitidas en contingencia (TipoEmision=2).
        /// Gap 12: el método SOAP singular `recepcionFactura` SOLO acepta CodigoEmision=1
        /// (online) — SIAT rechazaba con 916 cuando le mandábamos 2. La forma correcta
        /// de reenviar contingencia es usar `recepcionPaqueteFactura` (masiva) con 1 venta,
        /// que SÍ acepta CodigoEmision=2 y el `codigoEvento` para la asociación factura↔evento.
        ///
        /// Esta operación delega a <see cref="EnviarRecepcionPaqueteContingenciaAsync"/>
        /// con un paquete de 1 sola venta. Devuelve <see cref="RespuestaRecepcionFacturaDto"/>
        /// por compatibilidad con el caller legacy.
        ///
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        public async Task<RespuestaRecepcionFacturaDto> EnviarRecepcionContingenciaAsync(
            Venta venta,
            EventoSignificativoSiat evento,
            CancellationToken ct = default)
        {
            var respuestaPaquete = await EnviarRecepcionPaqueteContingenciaAsync(
                new[] { venta }, evento, ct);

            // Mapear respuesta del paquete a la respuesta singular (mismo shape).
            return new RespuestaRecepcionFacturaDto
            {
                Transaccion = respuestaPaquete.Transaccion,
                CodigoEstado = respuestaPaquete.CodigoEstado,
                CodigoRecepcion = respuestaPaquete.CodigoRecepcion,
                CodigoDescripcion = respuestaPaquete.CodigoDescripcion,
                CodigosRespuesta = respuestaPaquete.CodigosRespuesta
            };
        }

        /// <summary>
        /// Devuelve el CUIS vigente cacheado sin forzar refresh. En contingencia
        /// el CUIS puede estar vencido (la asociación es por codigoRecepcionEventoSignificativo,
        /// no por vigencia de CUIS), así que preferimos un CUIS cacheado antes que
        /// fallar el reenvío por intentar renovarlo contra un SIAT que podría estar
        /// intermitente.
        /// </summary>
        private async Task<string> ObtenerCuisCacheadoAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct)
        {
            try
            {
                var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                    codigoSucursal, codigoPuntoVenta, ct);
                return cuis.Codigo;
            }
            catch
            {
                // Si no podemos resolver CUIS (p.ej. la BD está vacía), el SIAT
                // rechazará la factura contingencia. Eso es fail-closed y
                // aceptable: el operador verá el error y podrá investigar.
                return string.Empty;
            }
        }

        /// <summary>
        /// Envía un paquete de N facturas contingencia a <c>recepcionPaqueteFactura</c>
        /// (operación SOAP masiva del ServicioFacturacionCompraVenta).
        ///
        /// Esta es la vía oficial para que la asociación factura↔evento significativo
        /// quede registrada en el sobre SOAP. <paramref name="evento"/> debe tener
        /// <c>CodigoRecepcionEventoSignificativo</c> poblado — eso viaja como
        /// <c>codigoEvento</c> (long) en el request. La operación singular
        /// <c>recepcionFactura</c> NO incluye ese campo; queda sólo para reenvío de
        /// contingencia online (1 factura) si se decidiera mantener.
        ///
        /// Pre-condiciones (validadas por el caller, típicamente
        /// <see cref="ReenvioFacturasContingenciaService"/>):
        /// - Todas las ventas comparten
        ///   (CodigoSucursal, CodigoPuntoVenta, EventoSignificativoSiatId, Cufd).
        /// - 1 &lt;= ventas.Count &lt;= CantidadMaximaPaquete.
        /// - Cada venta tiene XmlBase64 (gzip+base64) y CodigoHash poblados.
        /// - evento.CodigoRecepcionEventoSignificativo poblado (es lo que viaja como codigoEvento).
        ///
        /// Si el SIAT rechaza (Transaccion=false) las ventas quedan en estado
        /// Pendiente con ErrorMensaje — eso lo hace el caller. Este método solo
        /// prepara y envía.
        ///
        /// Ver [[kafeyana-contingencia-paquete-siat]].
        /// </summary>
        public async Task<RespuestaRecepcionPaqueteFacturaDto> EnviarRecepcionPaqueteContingenciaAsync(
            IReadOnlyList<Venta> ventas,
            EventoSignificativoSiat evento,
            CancellationToken ct = default)
        {
            if (ventas is null || ventas.Count == 0)
                throw new ArgumentException("La lista de ventas no puede estar vacía.", nameof(ventas));

            if (ventas.Count > _opts.CantidadMaximaPaquete)
                throw new ArgumentException(
                    $"El paquete tiene {ventas.Count} facturas; máximo permitido es {_opts.CantidadMaximaPaquete}.",
                    nameof(ventas));

            if (string.IsNullOrWhiteSpace(evento.CodigoRecepcionEventoSignificativo))
                throw new InvalidOperationException(
                    $"Evento {evento.Id} sin CodigoRecepcionEventoSignificativo — no se puede paquetizar.");

            var muestra = ventas[0];

            // Inicializar correlation ID para este intento de paquete. Se propaga
            // automáticamente por AsyncLocal a SiatHttpClient.EnviarSoapAsync y a
            // todos los logs del archivo contingencia-{fecha}.log.
            var correlationId = Guid.NewGuid().ToString("N")[..16];
            _debug.BeginScope(correlationId);

            // Cabecera del intento en el archivo de log.
            _debug.LogInfo("RecepcionFacturaService", "inicio_paquete",
                $"ventas={ventas.Count} eventoId={evento.Id} codEmision=2 "
              + $"suc={muestra.CodigoSucursal} pv={muestra.CodigoPuntoVenta} "
              + $"cufd={muestra.Cufd} codRecepEvento={evento.CodigoRecepcionEventoSignificativo}");

            // 1. Armar el archivo (base64Binary). Gap 11: SIAT espera un TAR.GZ con
            //    un entry .xml por factura (validado contra piloto jun-2026 con 13
            //    variantes; ver [[kafeyana-contingencia-paquete-siat]]). El hash debe
            //    calcularse sobre los bytes del TAR.GZ, no sobre el base64.
            var (archivoB64, hash) = ArmarArchivoPaquete(ventas);

            // Log a archivo contingencia: magic bytes + hash + N entries del tar.
            // Reemplaza el bloque /tmp/ previo (no funciona en Windows production).
            try
            {
                var tarGzBytes = Convert.FromBase64String(archivoB64);
                var magicHex = tarGzBytes.Length >= 4
                    ? BitConverter.ToString(tarGzBytes, 0, 4)
                    : "(<4 bytes)";
                var (entries, bytesDescomprimidos) = ContarEntriesTarGz(tarGzBytes);

                _debug.LogArchivoArmado("TAR.GZ", tarGzBytes.Length, magicHex, hash, entries, bytesDescomprimidos);

                // Mantener el log ILogger existente para no romper la trazabilidad
                // actual vía Seq/consola (sin persistir el archivo a /tmp/ — eso
                // era el problema original).
                _logger.LogInformation(
                    "RecepcionPaqueteContingencia: ventas={Count}, tarGzBytes={Len}, "
                  + "magicBytes={Magic} (esperado '1F-8B-08' = TAR.GZ gzip), "
                  + "entries={Entries}, hashArchivo={Hash}",
                    ventas.Count, tarGzBytes.Length, magicHex, entries, hash);
            }
            catch (Exception ex)
            {
                _debug.LogWarn("RecepcionFacturaService", "log_archivo_armado_failed",
                    $"no se pudo inspeccionar el archivo armado: {ex.Message}", ex);
            }

            // CRÍTICO (WSDL confirmada el 28-jun-2026): codigoEvento es xs:long y
            // es el CodigoRecepcionEventoSignificativo que devolvió el SIAT al
            // registrar el evento (NO el CodigoMotivo 1-7).
            //
            // FIX #3: el campo se persiste crudo desde la respuesta SIAT. Si por
            // algún motivo llega no numérico (prefijo, espacio interno, respuesta
            // SOAP malformada), long.Parse lanza FormatException no controlada
            // y rompe todo el paquete. Defensivo: TryParse + VentaException
            // claro. Ver [[kafeyana-vservices-throw-on-missing-config]].
            if (!long.TryParse(evento.CodigoRecepcionEventoSignificativo, out var codigoEvento))
            {
                _debug.LogError("RecepcionFacturaService", "codigo_evento_malformado",
                    $"eventoId={evento.Id} codRecepEventoRaw='{evento.CodigoRecepcionEventoSignificativo}'", null);
                throw new VentaException(
                    $"Evento {evento.Id} tiene CodigoRecepcionEventoSignificativo malformado: "
                  + $"'{evento.CodigoRecepcionEventoSignificativo}'. Corregir manualmente en BD antes de reintentar.");
            }

            if (codigoEvento == 0)
            {
                _debug.LogError("RecepcionFacturaService", "codigo_evento_cero",
                    $"eventoId={evento.Id} codRecepEventoRaw='{evento.CodigoRecepcionEventoSignificativo}'", null);
                throw new VentaException(
                    $"Evento {evento.Id} tiene CodigoRecepcionEventoSignificativo = '0'. "
                  + "SIAT acepta el paquete pero no lo acredita como prueba INACCESIBILIDAD. "
                  + "Verificar que registroEventoSignificativo devolvió un código válido y que la BD lo persistió correctamente.");
            }

            // 2. Armar la solicitud SOAP.
            var solicitud = new SolicitudRecepcionPaqueteFacturaDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSector,
                CodigoEmision = 2, // Contingencia fijo
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = muestra.CodigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = muestra.CodigoSucursal,
                Cufd = muestra.Cufd ?? string.Empty,
                Cuis = await ObtenerCuisCacheadoAsync(muestra.CodigoSucursal, muestra.CodigoPuntoVenta, ct),
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                Archivo = archivoB64,
                HashArchivo = hash,
                FechaEnvio = DateTime.UtcNow,
                CodigoEvento = codigoEvento,
                CantidadFacturas = ventas.Count,
                Cafc = ventas.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Cafc))?.Cafc,
                // CodigoMotivo del evento significativo (1-7). El HTTP client usa esto
                // para decidir si incluir <cafc>: solo para motivos 5/6/7 (manual/talonario).
                CodigoMotivo = evento.CodigoMotivo
            };

            // Log a archivo contingencia: campos relevantes de la solicitud (sin
            // el base64 completo — sería ilegible; mostramos preview truncado).
            var archivoB64Preview = archivoB64.Length <= 200
                ? archivoB64
                : archivoB64[..100] + $"...[{archivoB64.Length - 200} chars ocultos]..." + archivoB64[^100..];
            _debug.LogInfo("RecepcionFacturaService", "solicitud_armada",
                $"archivoB64_len={archivoB64.Length} archivoB64_preview={archivoB64Preview} "
              + $"hashArchivo={hash} codigoEvento={codigoEvento} cantidadFacturas={ventas.Count} "
              + $"caf={(string.IsNullOrWhiteSpace(solicitud.Cafc) ? "(vacío)" : solicitud.Cafc)}");

            // 3. Enviar y loguear.
            var respuesta = await _siat.RecepcionPaqueteFacturaAsync(solicitud, ct);

            // Log a archivo contingencia: respuesta SIAT parseada.
            var erroresStr = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));
            _debug.LogInfo("RecepcionFacturaService", "respuesta_recibida",
                $"transaccion={respuesta.Transaccion} codRecep={respuesta.CodigoRecepcion} "
              + $"codEstado={respuesta.CodigoEstado} codDescripcion={respuesta.CodigoDescripcion} "
              + $"errores={erroresStr}");

            if (!respuesta.Transaccion)
            {
                _logger.LogWarning(
                    "SIAT rechazó RecepcionPaqueteFactura (eventoId={EventoId}, lote={Count}). Estado={Estado}. Errores: {Errores}",
                    evento.Id, ventas.Count, respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(erroresStr) ? respuesta.CodigoDescripcion : erroresStr);
            }
            else
            {
                _logger.LogInformation(
                    "RecepcionPaqueteFactura aceptada (eventoId={EventoId}, lote={Count}). CodigoRecepcion={CodRecep}",
                    evento.Id, ventas.Count, respuesta.CodigoRecepcion);
            }

            return respuesta;
        }

        /// <summary>
        /// FIX #1 — implementación de <c>ValidarRecepcionPaqueteContingenciaAsync</c>.
        /// Construye la solicitud SOAP <c>validacionRecepcionPaqueteFactura</c> y delega
        /// en <c>SiatHttpClient.ValidacionRecepcionPaqueteFacturaAsync</c>. Se usa desde
        /// <c>ReenvioFacturasContingenciaService.MapearRespuestaPaqueteAsync</c> para
        /// esperar el estado definitivo (908) antes de marcar las ventas como Validadas.
        /// Token y módulo se leen de <c>SiatOptions</c>; el <c>cuis</c> lo trae el caller
        /// porque puede ser distinto entre paquetes del mismo evento.
        /// </summary>
        public async Task<RespuestaValidacionRecepcionPaqueteDto> ValidarRecepcionPaqueteContingenciaAsync(
            string codigoRecepcion,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoAmbiente,
            string cuis,
            string cufd,
            string codigoSistema,
            long nit,
            CancellationToken ct = default)
        {
            // FIX #8 — codigoRecepcion viene como GUID/hex del piloto SIAT (no long).
            // Sólo validamos que no esté vacío, sin asumir formato numérico.
            if (string.IsNullOrWhiteSpace(codigoRecepcion))
                throw new ArgumentException("codigoRecepcion es requerido", nameof(codigoRecepcion));
            if (string.IsNullOrWhiteSpace(cuis))
                throw new ArgumentException("cuis es requerido", nameof(cuis));
            if (string.IsNullOrWhiteSpace(cufd))
                throw new ArgumentException("cufd es requerido (FIX #10: el piloto exige el CUFD del momento del envío en validacionRecepcionPaqueteFactura)", nameof(cufd));
            if (string.IsNullOrWhiteSpace(codigoSistema))
                codigoSistema = _opts.CodigoSistema;

            var solicitud = new ValidacionRecepcionPaqueteDto
            {
                // FIX #10 — orden del XSD del piloto + 5 campos que faltaban.
                // Antes solo se mandaban 9 elementos y 2 unexpected (CodigoModulo,
                // Token) → el SIAT respondía 500 con Unmarshalling Error.
                Cuis = cuis,
                CodigoAmbiente = codigoAmbiente,
                CodigoPuntoVenta = codigoPuntoVenta,
                // FIX #11 — ValidarRecepcionPaqueteContingencia SOLO valida
                // paquetes contingencia, así que codigoEmision SIEMPRE es 2.
                // Antes se tomaba _opts.CodigoEmision (1 online) y el SIAT
                // rechazaba con [916] TIPO DE EMISION ES INVALIDO.
                CodigoEmision = 2,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                CodigoSistema = codigoSistema,
                Nit = nit,
                CodigoSucursal = codigoSucursal,
                CodigoDocumentoSector = _opts.CodigoDocumentoSector,
                Cufd = cufd,                          // CUFD del momento del envío, no el vigente
                CodigoRecepcion = codigoRecepcion,
                CodigoModalidad = _opts.CodigoModalidad,
            };

            var respuesta = await _siat.ValidacionRecepcionPaqueteFacturaAsync(solicitud, ct);

            // DIAG: serializar mensajesList para entender el 904 (NO cambia lógica de negocio,
            // solo expone el detalle de observación que el SIAT devuelve junto al codigoEstado).
            var mensajesStr = string.Join(" | ", respuesta.MensajesList
                .Select(m => $"[arch={m.NumeroArchivo ?? "?"} det={m.NumeroDetalle ?? "?"} "
                           + $"cod={m.Codigo ?? "?"} adv={m.Advertencia ?? "?"}] "
                           + $"{m.Descripcion ?? "(sin descripción)"}"));

            _logger.LogInformation(
                "FIX #1 ValidarRecepcionPaqueteContingencia: codigoRecepcion={CodRecep}, "
              + "codigoEstado={Estado}, transaccion={Tx}, mensajes=[{Mensajes}]",
                respuesta.CodigoRecepcion, respuesta.CodigoEstado, respuesta.Transaccion, mensajesStr);

            return respuesta;
        }

        /// <summary>
        /// Helper privado: arma el campo <c>archivo</c> (base64Binary) del paquete
        /// contingencia para la operación SOAP <c>recepcionPaqueteFactura</c>.
        ///
        /// GAP 11 (jun-2026): validado contra el piloto SIAT con 13 variantes, el
        /// SIAT espera un archivo <b>TAR.GZ</b> con un entry por factura (no un
        /// ZIP, no un gzip simple con 1 XML). El nombre del entry no importa:
        /// <c>factura.xml</c>, <c>{CUF}.xml</c>, <c>facturaComputarizada.xml</c>
        /// e incluso <c>facturas/factura.xml</c> son todos aceptados. La
        /// <c>cantidadFacturas</c> declarada en el sobre debe coincidir con el N
        /// de entries en el tar (validado con 985 "LA CANTIDAD DE FACTURAS ES
        /// DIFERENTE A LA DECLARADA" al probar TAR.GZ con 2 entries + cant=1).
        ///
        /// Variantes descartadas (todas rechazadas con [920] "No se desempaqueto
        /// XMLs"): gzip(1 XML), ZIP STORED, ZIP DEFLATE, ZIP con varios
        /// entries/firma/MANIFIESTO, XML crudo, XML con BOM, header propietario
        /// + gzip. Solo TAR.GZ dio <c>transaccion=true</c>.
        ///
        /// Ver [[kafeyana-contingencia-paquete-siat]] y [[kafeyana-siat-gap-920]].
        /// </summary>
        private static (string archivoB64, string hash) ArmarArchivoPaquete(IReadOnlyList<Venta> ventas)
        {
            if (ventas.Count == 0)
                throw new ArgumentException("Lista vacía.", nameof(ventas));

            using var tarGzMs = new MemoryStream();
            using (var gz = new GZipStream(tarGzMs, CompressionLevel.Optimal, leaveOpen: true))
            using (var tar = new TarWriter(gz, TarEntryFormat.Pax, leaveOpen: true))
            {
                for (int i = 0; i < ventas.Count; i++)
                {
                    var v = ventas[i];
                    if (string.IsNullOrWhiteSpace(v.XmlBase64))
                        throw new InvalidOperationException(
                            $"Venta {v.Id} sin XmlBase64 — no se puede incluir en paquete.");

                    // GAP 11: descomprimir el gzip(xml) persistido en BD para obtener
                    // el XML crudo UTF-8 que va como entry del tar. El SIAT procesa
                    // cada entry como una factura independiente.
                    string xmlCrudo = SiatGzip.DescomprimirBase64(v.XmlBase64);
                    var bytes = Encoding.UTF8.GetBytes(xmlCrudo);

                    // Nombre del entry: usamos el CUF para tener trazabilidad
                    // (SIAT acepta cualquier nombre .xml, validado en piloto).
                    var entryName = $"{v.Cuf ?? $"factura_{v.Id}"}.xml";

                    // .NET 9 TarWriter: solo expone WriteEntry(TarEntry) y
                    // WriteEntry(string fileName, string entryName). Para escribir
                    // bytes en memoria, creamos el PaxTarEntry y le asignamos el
                    // DataStream (heredado de TarEntry). Ver docs:
                    // https://learn.microsoft.com/dotnet/api/system.formats.tar.tarwriter.writeentry
                    var entry = new PaxTarEntry(TarEntryType.RegularFile, entryName);
                    entry.DataStream = new MemoryStream(bytes);
                    tar.WriteEntry(entry);
                }
            }

            // tarGzMs contiene TAR.GZ cerrado y flusheado. Calcular hash sobre
            // los bytes (no sobre el base64) — patrón del fix GAP 11.f.
            var tarGzBytes = tarGzMs.ToArray();
            var archivoB64 = Convert.ToBase64String(tarGzBytes, Base64FormattingOptions.None);
            var hash = SiatSha256.Generar(tarGzBytes);
            return (archivoB64, hash);
        }

        /// <summary>
        /// FIX GAP 11.f retroactivo: limpia el base64 de <paramref name="archivo"/>
        /// (remueve \r\n/espacios introducidos por versiones viejas de SiatGzip) y
        /// recalcula el hash sobre los bytes gzip resultantes.
        ///
        /// Garantiza que TODO lo que sale del backend hacia SIAT — incluso ventas
        /// persistidas antes del fix de SiatGzip.cs — llegue con base64 sin
        /// whitespace y con un hash consistente sobre los bytes del archivo.
        ///
        /// Idéntico patrón al que ya usa ArmarArchivoPaquete (contingencia),
        /// unificado para que el flujo online singular quede igual de robusto.
        /// Ver [[kafeyana-siat-gap-920]].
        /// </summary>
        private static (string archivo, string hash) SanitizarArchivoYHash(
            string archivo, string? hashArchivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                return (archivo, hashArchivo ?? string.Empty);

            // Convert.FromBase64String ignora whitespace (\r, \n, espacios, tabs),
            // así que es seguro aunque el archivo venga "sucio".
            var bytes = Convert.FromBase64String(archivo);
            var archivoLimpio = Convert.ToBase64String(bytes, Base64FormattingOptions.None);
            // Recalculamos SIEMPRE el hash desde los bytes gzip, descartando cualquier
            // hash previo. Esto es lo que SIAT espera y mantiene consistencia entre
            // ventas nuevas y viejas.
            var hashLimpio = SiatSha256.Generar(bytes);
            return (archivoLimpio, hashLimpio);
        }

        /// <summary>
        /// Inspecciona un TAR.GZ armado por <see cref="ArmarArchivoPaquete"/> y
        /// devuelve la cantidad de entries del tar más el total de bytes
        /// descomprimidos (sumando la longitud de cada DataStream).
        ///
        /// Usado SOLO por el log de diagnóstico de contingencia (ver
        /// [[kafeyana-contingencia-siat]]). No se calcula en el camino crítico de
        /// armado: el SIAT recibe los bytes tal cual y rechaza con error 920 si
        /// algo está mal. Este helper existe para que el operador vea en el log
        /// cuántos XMLs efectivamente se empaquetaron y qué tamaño tenían
        /// descomprimidos, sin tener que desempaquetarlos a mano con `tar -tzvf`.
        ///
        /// Si el archivo está malformado o no es un TAR.GZ, devuelve (0, 0) y
        /// deja que el catch del caller loguee el motivo — NUNCA lanza.
        /// </summary>
        private static (int entries, long bytesDescomprimidos) ContarEntriesTarGz(byte[] tarGzBytes)
        {
            if (tarGzBytes is null || tarGzBytes.Length < 4)
                return (0, 0);

            using var ms = new MemoryStream(tarGzBytes);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);

            int count = 0;
            long bytes = 0;
            using var reader = new TarReader(gz);
            TarEntry? entry;
            while ((entry = reader.GetNextEntry()) is not null)
            {
                count++;
                // PaxTarEntry expone Length (tamaño original del archivo).
                // Para RegularFile sin Length reportado, contamos el DataStream
                // hasta EOF como fallback.
                if (entry.Length > 0)
                {
                    bytes += entry.Length;
                }
                else if (entry.DataStream is not null && entry.DataStream.CanRead)
                {
                    long len = 0;
                    var buf = new byte[4096];
                    int n;
                    while ((n = entry.DataStream.Read(buf, 0, buf.Length)) > 0)
                        len += n;
                    bytes += len;
                }
            }
            return (count, bytes);
        }
    }
}