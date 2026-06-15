using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class RecepcionFacturaService : IRecepcionFacturaService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<RecepcionFacturaService> _logger;

        public RecepcionFacturaService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<RecepcionFacturaService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
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
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                throw new ArgumentException("El archivo de factura es requerido.", nameof(archivo));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                _opts.CodigoSucursal, _opts.CodigoPuntoVenta, ct);

            var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                _opts.CodigoSucursal, _opts.CodigoPuntoVenta, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de facturar.");

            if (!cufd.EsVigente())
                throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de facturar.");

            hashArchivo = string.IsNullOrWhiteSpace(hashArchivo)
                ? CalcularHashArchivo(archivo)
                : hashArchivo.Trim();

            var solicitud = new SolicitudRecepcionFacturaDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = _opts.CodigoDocumentoSector,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = _opts.CodigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = _opts.CodigoSucursal,
                Cufd = cufd.Codigo,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                Archivo = archivo,
                HashArchivo = hashArchivo,
                FechaEnvio = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Solicitud RecepcionFactura preparada. HashArchivo={Hash}. CUIS vigente hasta {CuisVigencia}, CUFD vigente hasta {CufdVigencia}",
                hashArchivo,
                cuis.FechaVigencia,
                cufd.FechaVigencia);

            return solicitud;
        }

        public async Task<RespuestaRecepcionFacturaDto> EnviarRecepcionAsync(
            string archivo,
            string? hashArchivo = null,
            CancellationToken ct = default)
        {
            var dto = await PrepararSolicitudAsync(archivo, hashArchivo, ct);
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
    }
}
