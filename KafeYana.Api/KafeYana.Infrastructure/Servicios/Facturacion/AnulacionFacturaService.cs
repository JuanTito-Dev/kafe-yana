using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class AnulacionFacturaService : IAnulacionFacturaService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<AnulacionFacturaService> _logger;

        public AnulacionFacturaService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<AnulacionFacturaService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<SolicitudAnulacionFacturaDto> PrepararSolicitudAsync(
            string cuf,
            int codigoMotivo,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoDocumentoSector,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cuf))
                throw new ArgumentException("El CUF de la factura es requerido.", nameof(cuf));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
            // Para anulación usamos la hora UTC actual como fechaEmision del CUFD.
            var fechaEmisionAnulacion = SiatFechaEmision.AhoraUtc();
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(codigoSucursal, codigoPuntoVenta, fechaEmisionAnulacion, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de anular la factura.");

            if (!cufd.EsVigente())
                throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de anular la factura.");

            var solicitud = new SolicitudAnulacionFacturaDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = codigoDocumentoSector,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = codigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = codigoSucursal,
                Cufd = cufd.Codigo,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumento,
                CodigoMotivo = codigoMotivo,
                Cuf = cuf.Trim()
            };

            _logger.LogInformation(
                "Solicitud AnulacionFactura preparada. CUF={Cuf}, Motivo={Motivo}",
                solicitud.Cuf,
                solicitud.CodigoMotivo);

            return solicitud;
        }

        public async Task<RespuestaAnulacionFacturaDto> EnviarAnulacionAsync(
            SolicitudAnulacionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var respuesta = await _siat.AnulacionFacturaAsync(solicitud, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó AnulacionFactura. CUF={Cuf}. Estado={Estado}. Mensajes: {Errores}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "AnulacionFactura aceptada por SIAT. CUF={Cuf}. Estado={Estado}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado);
            }

            return respuesta;
        }
    }
}
