using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class CuisService : ICuisService
    {
        private readonly SiatHttpClient _siat;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<CuisService> _logger;

        public CuisService(
            SiatHttpClient siat,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<CuisService> logger)
        {
            _siat = siat;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<Cuis> ObtenerCuisAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var resp = await _siat.SolicitarCuisAsync(codigoSucursal, codigoPuntoVenta, ct);

            if (string.IsNullOrWhiteSpace(resp.CodigoCuis))
            {
                var errores = FormatearErroresSiat(resp.CodigosRespuesta);
                _logger.LogWarning(
                    "SIAT sin código CUIS. transaccion={Transaccion}. Mensajes: {Errores}",
                    resp.Transaccion,
                    errores);
                throw new InvalidOperationException($"SIAT rechazó CUIS: {errores}");
            }

            if (!resp.Transaccion)
            {
                _logger.LogInformation(
                    "SIAT devolvió CUIS existente (transaccion=false). Codigo: {Codigo}",
                    resp.CodigoCuis);
            }

            var cuis = new Cuis
            {
                Codigo = resp.CodigoCuis,
                FechaVigencia = NormalizarUtc(resp.FechaVigencia ?? DateTime.UtcNow.AddYears(1)),
                CodigoSucursal = codigoSucursal,
                CodigoPuntoVenta = codigoPuntoVenta,
                FechaRegistro = DateTime.UtcNow
            };

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.Cuis.Add(cuis);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CUIS obtenido del SIAT y guardado (Id:{Id}). Vigente hasta: {Vigencia}",
                cuis.Id,
                cuis.FechaVigencia.ToString("yyyy-MM-dd HH:mm:ss"));

            return cuis;
        }

        public async Task<Cuis> ObtenerCuisVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var vigente = await db.Cuis
                .Where(c =>
                    c.CodigoSucursal == codigoSucursal
                    && c.CodigoPuntoVenta == codigoPuntoVenta
                    && c.FechaVigencia > DateTime.UtcNow)
                .OrderByDescending(c => c.FechaRegistro)
                .FirstOrDefaultAsync(ct);

            if (vigente is not null)
            {
                _logger.LogDebug(
                    "CUIS vigente desde BD (Id:{Id}). Vigente hasta: {V}",
                    vigente.Id,
                    vigente.FechaVigencia);
                return vigente;
            }

            _logger.LogWarning("CUIS vencido o inexistente en BD → solicitando al SIAT...");
            return await ObtenerCuisAsync(codigoSucursal, codigoPuntoVenta, ct);
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
