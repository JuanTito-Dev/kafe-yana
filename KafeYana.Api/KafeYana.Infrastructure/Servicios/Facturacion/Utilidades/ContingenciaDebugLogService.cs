using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using KafeYana.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>
    /// Logger a archivo rotativo por día para el flujo de contingencia SIAT.
    ///
    /// Complementa al <see cref="ILogger"/> estándar (consola + Seq). Se activa
    /// específicamente para diagnóstico cuando el envío falla: persiste el
    /// armado del paquete, el SOAP request/response, el HTTP status y la decisión
    /// final del flujo. Ver [[kafeyana-contingencia-siat]].
    ///
    /// Diseño:
    /// <list type="bullet">
    ///   <item>Un único archivo por día: <c>{DebugLogPath}/contingencia-{yyyy-MM-dd}.log</c></item>
    ///   <item>UTF-8 sin BOM, append-only (no bloquea a otros lectores)</item>
    ///   <item>Líneas con timestamp UTC + correlation ID + nivel + componente</item>
    ///   <item>Payloads multi-línea (SOAP XML, base64 truncado) en bloques indentados</item>
    ///   <item><see cref="SemaphoreSlim"/> por instancia para serializar writes concurrentes</item>
    ///   <item>Singleton — el lifecycle del StreamWriter se mantiene en memoria</item>
    /// </list>
    /// </summary>
    public interface IContingenciaDebugLogService
    {
        /// <summary>Correlation ID ambient actual (fallback a "(none)").</summary>
        string CurrentCorrelationId { get; }

        /// <summary>Setea el correlation ID ambient para toda la cadena async.</summary>
        void BeginScope(string correlationId);

        /// <summary>Limpia el correlation ID ambient (al salir del scope lógico).</summary>
        void EndScope();

        /// <summary>Línea de info (evento normal del flujo).</summary>
        void LogInfo(string componente, string accion, string? datos = null);

        /// <summary>Línea de warning (irregularidad recuperable).</summary>
        void LogWarn(string componente, string accion, string datos, Exception? ex = null);

        /// <summary>Línea de error (falla que el SIAT rechaza o excepción no controlada).</summary>
        void LogError(string componente, string accion, string datos, Exception? ex = null);

        /// <summary>SOAP request XML completo (formateado e indentado si tiene tamaño razonable).</summary>
        void LogSoapRequest(string operacion, string xmlCompleto, string urlDestino);

        /// <summary>SOAP response XML completo + HTTP status + apikey mask.</summary>
        void LogSoapResponse(string operacion, int httpStatus, string xmlCompleto, string apikeyMasked);

        /// <summary>Línea estructurada con magic + hash + entries del tar/ZIP armado.</summary>
        void LogArchivoArmado(string tipoPaquete, int bytes, string magicHex, string sha256, int entries, long bytesDescomprimidos);

        /// <summary>Helper para enmascarar API keys (primeros 12 + "..." + últimos 6 chars).</summary>
        string MaskApikey(string? apikey);
    }

    public sealed class ContingenciaDebugLogService : IContingenciaDebugLogService
    {
        private readonly SiatOptions _opts;
        private readonly ILogger<ContingenciaDebugLogService> _logger;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private string? _rutaArchivoActual;
        private DateTime _fechaActual;

        public ContingenciaDebugLogService(
            IOptions<SiatOptions> opts,
            ILogger<ContingenciaDebugLogService> logger)
        {
            _opts = opts.Value;
            _logger = logger;
        }

        public string CurrentCorrelationId => ContingenciaContext.CorrelationId;

        public void BeginScope(string correlationId) =>
            ContingenciaContext.CorrelationId = correlationId;

        public void EndScope() =>
            ContingenciaContext.CorrelationId = null!;

        public void LogInfo(string componente, string accion, string? datos = null) =>
            EscribirLinea("INFO ", componente, accion, datos, null);

        public void LogWarn(string componente, string accion, string datos, Exception? ex = null) =>
            EscribirLinea("WARN ", componente, accion, datos, ex);

        public void LogError(string componente, string accion, string datos, Exception? ex = null) =>
            EscribirLinea("ERROR", componente, accion, datos, ex);

        public void LogSoapRequest(string operacion, string xmlCompleto, string urlDestino)
        {
            var bloque = new StringBuilder();
            bloque.AppendLine($"{Prefijo("INFO ", "SiatHttpClient", "soap_request")} operacion={operacion} url={urlDestino} bytes={Encoding.UTF8.GetByteCount(xmlCompleto)}");
            bloque.Append(IndentarXml(xmlCompleto));
            EscribirBloque(bloque.ToString());
        }

        public void LogSoapResponse(string operacion, int httpStatus, string xmlCompleto, string apikeyMasked)
        {
            var bloque = new StringBuilder();
            bloque.AppendLine($"{Prefijo("INFO ", "SiatHttpClient", "soap_response")} operacion={operacion} http={httpStatus} apikey={apikeyMasked} bytes={Encoding.UTF8.GetByteCount(xmlCompleto)}");
            bloque.Append(IndentarXml(xmlCompleto));
            EscribirBloque(bloque.ToString());
        }

        public void LogArchivoArmado(string tipoPaquete, int bytes, string magicHex, string sha256, int entries, long bytesDescomprimidos)
        {
            var datos = $"tipo={tipoPaquete} bytes={bytes} magicHex={magicHex} (esperado {(tipoPaquete == "TAR.GZ" ? "1F-8B-08" : tipoPaquete == "ZIP" ? "50-4B-03-04" : "?")}) "
                      + $"sha256={sha256} entries={entries} bytesDescomprimidos={bytesDescomprimidos}";
            EscribirLinea("INFO ", "RecepcionFacturaService", "archivo_armado", datos, null);
        }

        public string MaskApikey(string? apikey)
        {
            if (string.IsNullOrWhiteSpace(apikey) || apikey.Length < 20)
                return "(empty-or-short)";
            return apikey[..12] + "..." + apikey[^6..];
        }

        // ─────────────────────────────────────────────
        // Escritura
        // ─────────────────────────────────────────────

        private void EscribirLinea(string nivel, string componente, string accion, string? datos, Exception? ex)
        {
            if (!_opts.DebugLogEnabled) return;
            var sb = new StringBuilder();
            sb.Append(Prefijo(nivel, componente, accion));
            if (!string.IsNullOrEmpty(datos))
                sb.Append(' ').Append(datos);
            if (ex is not null)
                sb.Append(" ex=").Append(ex.GetType().Name).Append(": ").Append(Truncar(ex.Message, 1024));
            EscribirBloque(sb.ToString());
        }

        private void EscribirBloque(string bloqueMultiLinea)
        {
            if (!_opts.DebugLogEnabled) return;
            if (string.IsNullOrEmpty(bloqueMultiLinea)) return;

            // Truncar líneas individuales que excedan el techo.
            var lines = bloqueMultiLinea.Split('\n');
            var sb = new StringBuilder(bloqueMultiLinea.Length + 16);
            foreach (var linea in lines)
            {
                var sinCr = linea.TrimEnd('\r');
                if (Encoding.UTF8.GetByteCount(sinCr) > _opts.DebugLogMaxBytesPorLinea)
                    sb.Append(sinCr[..Math.Min(sinCr.Length, _opts.DebugLogMaxBytesPorLinea / 2)])
                      .Append("... [TRUNCADO ").Append(Encoding.UTF8.GetByteCount(sinCr)).Append(" bytes] ...\n");
                else
                    sb.Append(sinCr).Append('\n');
            }

            try
            {
                _writeLock.Wait();
                try
                {
                    var ruta = ObtenerRutaArchivoActual();
                    File.AppendAllText(ruta, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                // Nunca re-lanzar errores desde logging (cascada infinita).
                _logger.LogWarning(ex, "ContingenciaDebugLog: no se pudo escribir a archivo de log.");
            }
        }

        private string ObtenerRutaArchivoActual()
        {
            var hoyUtc = DateTime.UtcNow.Date;
            if (_rutaArchivoActual is null || _fechaActual != hoyUtc)
            {
                _rutaArchivoActual = Path.Combine(
                    _opts.DebugLogPath,
                    $"contingencia-{hoyUtc:yyyy-MM-dd}.log");
                _fechaActual = hoyUtc;
            }
            // Siempre recrear directorio — si el usuario lo borra mientras corre la app,
            // File.AppendAllText falla silenciosamente sin este CreateDirectory.
            Directory.CreateDirectory(_opts.DebugLogPath);
            return _rutaArchivoActual;
        }

        // ─────────────────────────────────────────────
        // Formato
        // ─────────────────────────────────────────────

        private static string Prefijo(string nivel, string componente, string accion) =>
            $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{ContingenciaContext.CorrelationId,-16}] {nivel,-5} {componente}.{accion}";

        private string IndentarXml(string xml)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var sb = new StringBuilder();
                using var writer = System.Xml.XmlWriter.Create(sb, new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = new UTF8Encoding(false)
                });
                doc.WriteTo(writer);
                writer.Flush();
                var result = sb.ToString();
                // Validar tamaño: si es chico, devolver pretty; si es grande, devolver raw.
                return Encoding.UTF8.GetByteCount(result) <= _opts.DebugLogMaxBytesPorLinea
                    ? result
                    : xml;
            }
            catch
            {
                return xml;
            }
        }

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";
    }
}
