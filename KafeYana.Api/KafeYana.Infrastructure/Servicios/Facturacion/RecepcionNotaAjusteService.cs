using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Servicio de recepción SOAP para notas de crédito/débito (operación "recepcionDocumentoAjuste").
    /// Espejo de RecepcionFacturaService — diferencia clave: el sobre NO lleva Cufd.
    /// </summary>
    public class RecepcionNotaAjusteService : IRecepcionNotaAjusteService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly SiatOptions _opts;
        private readonly ILogger<RecepcionNotaAjusteService> _logger;

        public RecepcionNotaAjusteService(
            SiatHttpClient siat,
            ICuisService cuisService,
            IOptions<SiatOptions> opts,
            ILogger<RecepcionNotaAjusteService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _opts = opts.Value;
            _logger = logger;
        }

        public string CalcularHashArchivo(string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de la nota es requerido para calcular el hash.", nameof(archivo));

            return SiatSha256.GenerarHashArchivo(archivo);
        }

        public async Task<SolicitudRecepcionNotaAjusteDto> PrepararSolicitudAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de la nota es requerido.", nameof(archivo));

            // Sucursal/PV efectivos: si el caller los prefijó (caso de la nota, donde
            // viene de venta.CodigoSucursal/PuntoVenta), se usan esos. Si no, se cae
            // a appsettings.json. Esto replica el patrón de RecepcionFacturaService
            // y arregla la divergencia 1002/1008 cuando la venta se cobró con un PV
            // distinto al de appsettings.
            var sucEfectiva = codigoSucursal ?? _opts.CodigoSucursal;
            var pvEfectivo = codigoPuntoVenta ?? _opts.CodigoPuntoVenta;

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(sucEfectiva, pvEfectivo, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de emitir la nota.");

            hashArchivo = string.IsNullOrWhiteSpace(hashArchivo)
                ? CalcularHashArchivo(archivo)
                : hashArchivo.Trim();

            var solicitud = new SolicitudRecepcionNotaAjusteDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSectorNotaAjuste,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = pvEfectivo,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = sucEfectiva,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumentoNotaAjuste,
                Archivo = archivo,
                HashArchivo = hashArchivo,
                FechaEnvio = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Solicitud RecepcionDocumentoAjuste preparada. HashArchivo={Hash}. Suc={Suc}, PV={PV}. CUIS vigente hasta {CuisVigencia}",
                hashArchivo,
                sucEfectiva, pvEfectivo,
                cuis.FechaVigencia);

            return solicitud;
        }

        public async Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            DateTime? fechaEmision = null,
            int? codigoSucursal = null,
            int? codigoPuntoVenta = null,
            CancellationToken ct = default)
        {
            var dto = await PrepararSolicitudAsync(archivo, hashArchivo, fechaEmision, codigoSucursal, codigoPuntoVenta, ct);
            var respuesta = await _siat.RecepcionDocumentoAjusteAsync(dto, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó RecepcionDocumentoAjuste. Estado={Estado}. Mensajes: {Errores}",
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "RecepcionDocumentoAjuste aceptada. CodigoRecepcion={Codigo}",
                    respuesta.CodigoRecepcion);
            }

            return respuesta;
        }

        /// <summary>
        /// Overload para reenvío de notas emitidas en contingencia (TipoEmision=2).
        /// Acepta el <c>codigoRecepcionEventoSignificativo</c> del evento activo,
        /// que debe viajar en el sobre SOAP para que el SIAT valide la asociación.
        /// Espejo de <see cref="RecepcionFacturaService.EnviarRecepcionContingenciaAsync"/>
        /// excepto que el sobre NO lleva Cufd (verificado contra el sobre del piloto).
        /// Ver [[kafeyana-contingencia-siat]].
        /// </summary>
        public async Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionContingenciaAsync(
            string archivo,
            string? hashArchivo,
            int codigoSucursal,
            int codigoPuntoVenta,
            string codigoRecepcionEventoSignificativo,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigoRecepcionEventoSignificativo))
                throw new InvalidOperationException(
                    "Para TipoEmision=2 (Contingencia) se requiere codigoRecepcionEventoSignificativo.");

            var hashFinal = string.IsNullOrWhiteSpace(hashArchivo)
                ? CalcularHashArchivo(archivo)
                : hashArchivo.Trim();

            var solicitud = new SolicitudRecepcionNotaAjusteDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSectorNotaAjuste,
                CodigoEmision = 2, // Contingencia — fijo. Para CodigoModalidad=2 (Computarizada), codigoEmision=2 es "Computarizada fuera de línea" según Resolución Normativa 102100000028.
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = codigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = codigoSucursal,
                // En contingencia el CUIS puede estar vencido; la asociación es por evento.
                // Usamos el cacheado en BD. Si no hay, enviamos vacío y el SIAT decide.
                Cuis = await ObtenerCuisCacheadoAsync(codigoSucursal, codigoPuntoVenta, ct),
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumentoNotaAjuste,
                Archivo = archivo,
                HashArchivo = hashFinal,
                FechaEnvio = DateTime.UtcNow,
                CodigoRecepcionEventoSignificativo = codigoRecepcionEventoSignificativo
            };

            var respuesta = await _siat.RecepcionDocumentoAjusteAsync(solicitud, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó RecepcionDocumentoAjuste (contingencia). Estado={Estado}. Mensajes: {Errores}",
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "RecepcionDocumentoAjuste contingencia aceptada. CodigoRecepcion={Cod}",
                    respuesta.CodigoRecepcion);
            }

            return respuesta;
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
                // rechazará la nota contingencia. Eso es fail-closed y
                // aceptable: el operador verá el error y podrá investigar.
                return string.Empty;
            }
        }
    }
}
