using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class ReversionAnulacionFacturaService : IReversionAnulacionFacturaService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<ReversionAnulacionFacturaService> _logger;

        public ReversionAnulacionFacturaService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<ReversionAnulacionFacturaService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<SolicitudReversionAnulacionFacturaDto> PrepararSolicitudAsync(
            string cuf,
            int codigoSucursal,
            int codigoPuntoVenta,
            int codigoDocumentoSector,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cuf))
                throw new ArgumentException("El CUF de la factura es requerido.", nameof(cuf));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
            // Para reversión de anulación usamos la hora UTC actual como fechaEmision del CUFD.
            var fechaEmisionReversion = SiatFechaEmision.AhoraUtc();
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(codigoSucursal, codigoPuntoVenta, fechaEmisionReversion, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de revertir la anulación.");

            if (!cufd.EsVigente())
                throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de revertir la anulación.");

            var solicitud = new SolicitudReversionAnulacionFacturaDto
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
                Cuf = cuf.Trim()
            };

            _logger.LogInformation(
                "Solicitud ReversionAnulacionFactura preparada. CUF={Cuf}",
                solicitud.Cuf);

            return solicitud;
        }

        public async Task<RespuestaReversionAnulacionFacturaDto> EnviarReversionAsync(
            SolicitudReversionAnulacionFacturaDto solicitud,
            CancellationToken ct = default)
        {
            var respuesta = await _siat.ReversionAnulacionFacturaAsync(solicitud, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó ReversionAnulacionFactura. CUF={Cuf}. Estado={Estado}. Mensajes: {Errores}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "ReversionAnulacionFactura aceptada por SIAT. CUF={Cuf}. Estado={Estado}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado);
            }

            return respuesta;
        }
    }
}