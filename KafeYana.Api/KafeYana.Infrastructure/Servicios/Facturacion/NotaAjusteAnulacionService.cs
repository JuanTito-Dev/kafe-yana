using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Capa SOAP de bajo nivel para anulación de notas de crédito/débito.
    /// Espejo de <see cref="AnulacionFacturaService"/>.
    /// </summary>
    public class NotaAjusteAnulacionService : INotaAjusteAnulacionService
    {
        private readonly SiatHttpClient _siat;
        private readonly IUnitWork _db;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<NotaAjusteAnulacionService> _logger;

        public NotaAjusteAnulacionService(
            SiatHttpClient siat,
            IUnitWork db,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<NotaAjusteAnulacionService> logger)
        {
            _siat = siat;
            _db = db;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<SolicitudAnulacionDocumentoAjusteDto> PrepararSolicitudAsync(
            int notaId,
            int codigoMotivo,
            CancellationToken ct = default)
        {
            var nota = await _db.notasAjuste.FindByIdAsync(notaId)
                ?? throw new ArgumentException($"NotaAjuste {notaId} no encontrada.", nameof(notaId));

            if (string.IsNullOrWhiteSpace(nota.Cuf))
                throw new ArgumentException("La nota no tiene CUF registrado.", nameof(notaId));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(nota.CodigoSucursal, nota.CodigoPuntoVenta, ct);
            // Para anulación usamos la hora UTC actual como fechaEmision del CUFD.
            var fechaEmisionAnulacion = SiatFechaEmision.AhoraUtc();
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(nota.CodigoSucursal, nota.CodigoPuntoVenta, fechaEmisionAnulacion, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de anular la nota.");

            if (!cufd.EsVigente())
                throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de anular la nota.");

            var solicitud = new SolicitudAnulacionDocumentoAjusteDto
            {
                CodigoAmbiente = _opts.CodigoAmbiente,
                CodigoDocumentoSector = nota.CodigoDocumentoSector,
                CodigoEmision = _opts.CodigoEmision,
                CodigoModalidad = _opts.CodigoModalidad,
                CodigoPuntoVenta = nota.CodigoPuntoVenta,
                CodigoSistema = _opts.CodigoSistema,
                CodigoSucursal = nota.CodigoSucursal,
                Cufd = cufd.Codigo,
                Cuis = cuis.Codigo,
                Nit = _opts.Nit,
                TipoFacturaDocumento = _opts.TipoFacturaDocumentoNotaAjuste,
                CodigoMotivo = codigoMotivo,
                Cuf = nota.Cuf.Trim()
            };

            _logger.LogInformation(
                "Solicitud AnulacionDocumentoAjuste preparada. NotaId={NotaId} CUF={Cuf} Motivo={Motivo}",
                nota.Id,
                solicitud.Cuf,
                solicitud.CodigoMotivo);

            return solicitud;
        }

        public async Task<RespuestaAnulacionDocumentoAjusteDto> EnviarAnulacionAsync(
            SolicitudAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default)
        {
            var respuesta = await _siat.AnulacionDocumentoAjusteAsync(solicitud, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó AnulacionDocumentoAjuste. CUF={Cuf}. Estado={Estado}. Mensajes: {Errores}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "AnulacionDocumentoAjuste aceptada por SIAT. CUF={Cuf}. Estado={Estado}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado);
            }

            return respuesta;
        }
    }
}