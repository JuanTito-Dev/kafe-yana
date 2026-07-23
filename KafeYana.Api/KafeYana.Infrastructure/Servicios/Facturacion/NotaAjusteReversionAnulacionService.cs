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
    /// Capa SOAP de bajo nivel para reversión de anulación de notas C/D.
    /// Espejo de <see cref="ReversionAnulacionFacturaService"/>.
    /// </summary>
    public class NotaAjusteReversionAnulacionService : INotaAjusteReversionAnulacionService
    {
        private readonly SiatHttpClient _siat;
        private readonly IUnitWork _db;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly SiatOptions _opts;
        private readonly ILogger<NotaAjusteReversionAnulacionService> _logger;

        public NotaAjusteReversionAnulacionService(
            SiatHttpClient siat,
            IUnitWork db,
            ICuisService cuisService,
            ICufdService cufdService,
            IOptions<SiatOptions> opts,
            ILogger<NotaAjusteReversionAnulacionService> logger)
        {
            _siat = siat;
            _db = db;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<SolicitudReversionAnulacionDocumentoAjusteDto> PrepararSolicitudAsync(
            int notaId,
            CancellationToken ct = default)
        {
            var nota = await _db.notasAjuste.FindByIdAsync(notaId)
                ?? throw new ArgumentException($"NotaAjuste {notaId} no encontrada.", nameof(notaId));

            if (string.IsNullOrWhiteSpace(nota.Cuf))
                throw new ArgumentException("La nota no tiene CUF registrado.", nameof(notaId));

            var cuis = await _cuisService.ObtenerCuisVigenteAsync(nota.CodigoSucursal, nota.CodigoPuntoVenta, ct);
            var fechaEmisionReversion = SiatFechaEmision.AhoraUtc();
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(nota.CodigoSucursal, nota.CodigoPuntoVenta, fechaEmisionReversion, ct);

            if (!cuis.EsVigente())
                throw new InvalidOperationException("CUIS vencido. Solicite uno nuevo antes de revertir la anulación de la nota.");

            if (!cufd.EsVigente())
                throw new InvalidOperationException("CUFD vencido. Solicite uno nuevo antes de revertir la anulación de la nota.");

            var solicitud = new SolicitudReversionAnulacionDocumentoAjusteDto
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
                Cuf = nota.Cuf.Trim()
            };

            _logger.LogInformation(
                "Solicitud ReversionAnulacionDocumentoAjuste preparada. NotaId={NotaId} CUF={Cuf}",
                nota.Id,
                solicitud.Cuf);

            return solicitud;
        }

        public async Task<RespuestaReversionAnulacionDocumentoAjusteDto> EnviarReversionAsync(
            SolicitudReversionAnulacionDocumentoAjusteDto solicitud,
            CancellationToken ct = default)
        {
            var respuesta = await _siat.ReversionAnulacionDocumentoAjusteAsync(solicitud, ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

                _logger.LogWarning(
                    "SIAT rechazó ReversionAnulacionDocumentoAjuste. CUF={Cuf}. Estado={Estado}. Mensajes: {Errores}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado,
                    string.IsNullOrWhiteSpace(errores) ? respuesta.CodigoDescripcion : errores);
            }
            else
            {
                _logger.LogInformation(
                    "ReversionAnulacionDocumentoAjuste aceptada por SIAT. CUF={Cuf}. Estado={Estado}",
                    solicitud.Cuf,
                    respuesta.CodigoEstado);
            }

            return respuesta;
        }
    }
}