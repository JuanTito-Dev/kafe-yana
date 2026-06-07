using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class CufdService : ICufdService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<CufdService> _logger;

        public CufdService(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<CufdService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<Cufd> SolicitarCufdAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
            var resp = await _siat.SolicitarCufdAsync(cuis.Codigo, codigoSucursal, codigoPuntoVenta, ct);

            if (string.IsNullOrWhiteSpace(resp.CodigoCufd))
            {
                var errores = FormatearErroresSiat(resp.CodigosRespuesta);
                _logger.LogWarning(
                    "SIAT sin código CUFD. transaccion={Transaccion}. Mensajes: {Errores}",
                    resp.Transaccion,
                    errores);
                throw new InvalidOperationException($"SIAT rechazó CUFD: {errores}");
            }

            if (!resp.Transaccion)
            {
                _logger.LogInformation(
                    "SIAT devolvió CUFD existente (transaccion=false). Codigo: {Codigo}",
                    resp.CodigoCufd);
            }

            var cufd = new Cufd
            {
                Codigo = resp.CodigoCufd,
                CodigoControl = resp.CodigoControl ?? string.Empty,
                Direccion = resp.Direccion ?? string.Empty,
                FechaVigencia = NormalizarUtc(resp.FechaVigencia ?? DateTime.UtcNow.AddHours(24)),
                CodigoSucursal = codigoSucursal,
                CodigoPuntoVenta = codigoPuntoVenta,
                FechaRegistro = DateTime.UtcNow
            };

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.Cufd.Add(cufd);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CUFD obtenido del SIAT y guardado (Id:{Id}). Vigente hasta: {Vigencia}",
                cufd.Id,
                cufd.FechaVigencia.ToString("yyyy-MM-dd HH:mm:ss"));

            return cufd;
        }

        public async Task<Cufd> ObtenerCufdVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var vigente = await db.Cufd
                .Where(c =>
                    c.CodigoSucursal == codigoSucursal
                    && c.CodigoPuntoVenta == codigoPuntoVenta
                    && c.FechaVigencia > DateTime.UtcNow)
                .OrderByDescending(c => c.FechaRegistro)
                .FirstOrDefaultAsync(ct);

            if (vigente is not null)
            {
                _logger.LogDebug(
                    "CUFD vigente desde BD (Id:{Id}). Vigente hasta: {V}",
                    vigente.Id,
                    vigente.FechaVigencia);
                return vigente;
            }

            _logger.LogWarning("CUFD vencido o inexistente en BD → solicitando al SIAT...");
            return await SolicitarCufdAsync(codigoSucursal, codigoPuntoVenta, ct);
        }

        private static DateTime NormalizarUtc(DateTime fecha) =>
            fecha.Kind switch
            {
                DateTimeKind.Utc => fecha,
                DateTimeKind.Local => fecha.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
            };

        private static string FormatearErroresSiat(IEnumerable<CodigoRespuesta> mensajes)
        {
            var errores = string.Join(" | ", mensajes
                .Where(m => m.Codigo != 0 || !string.IsNullOrWhiteSpace(m.Descripcion))
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            return string.IsNullOrWhiteSpace(errores)
                ? "sin mensajes del SIAT"
                : errores;
        }
    }
}
