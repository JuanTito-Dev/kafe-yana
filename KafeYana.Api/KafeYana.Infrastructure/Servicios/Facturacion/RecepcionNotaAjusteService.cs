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
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de la nota es requerido.", nameof(archivo));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                _opts.CodigoSucursal, _opts.CodigoPuntoVenta, ct);

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
                CodigoPuntoVenta = _opts.CodigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = _opts.CodigoSucursal,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumentoNotaAjuste,
                Archivo = archivo,
                HashArchivo = hashArchivo,
                FechaEnvio = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Solicitud RecepcionDocumentoAjuste preparada. HashArchivo={Hash}. CUIS vigente hasta {CuisVigencia}",
                hashArchivo,
                cuis.FechaVigencia);

            return solicitud;
        }

        public async Task<RespuestaRecepcionNotaAjusteDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            CancellationToken ct = default)
        {
            var dto = await PrepararSolicitudAsync(archivo, hashArchivo, ct);
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
    }
}
