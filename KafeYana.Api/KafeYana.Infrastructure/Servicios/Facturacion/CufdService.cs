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
        /// <summary>
        /// Tolerancia entre la FechaEmisionSolicitud del CUFD y la fechaEmision
        /// del cobro actual. Si difieren más que esto, se descarta el CUFD viejo
        /// y se solicita uno nuevo al SIAT (porque el CUF que se generará debe
        /// coincidir con la fecha embebida en el CUFD).
        /// </summary>
        private static readonly TimeSpan ToleranciaReuso = TimeSpan.FromSeconds(2);

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
            DateTime fechaEmision,
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
                FechaRegistro = DateTime.UtcNow,
                // Guardamos la fechaEmision del SIAT con la que se pidió este CUFD.
                // Al generar el CUF después, deberemos usar EXACTAMENTE esta misma fecha,
                // si no el SIAT rechaza con 1002/1003.
                FechaEmisionSolicitud = NormalizarUtc(fechaEmision)
            };

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.Cufd.Add(cufd);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CUFD obtenido del SIAT y guardado (Id:{Id}). Vigente hasta: {Vigencia}. FechaEmisionSolicitud: {FechaSoli}",
                cufd.Id,
                cufd.FechaVigencia.ToString("yyyy-MM-dd HH:mm:ss"),
                cufd.FechaEmisionSolicitud.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            return cufd;
        }

        public async Task<Cufd> ObtenerCufdVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
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

            var fechaEmisionUtc = NormalizarUtc(fechaEmision);

            if (vigente is not null)
            {
                // Comparar la fechaEmision del cobro actual contra la fecha con la que
                // se pidió el CUFD. Si difieren más allá de la tolerancia, descartar.
                var diferencia = (vigente.FechaEmisionSolicitud - fechaEmisionUtc).Duration();
                if (diferencia <= ToleranciaReuso)
                {
                    _logger.LogDebug(
                        "CUFD vigente reusado (Id:{Id}). FechaEmisionSolicitud coincide con fechaEmision actual (Δ={Delta} ms)",
                        vigente.Id, (long)diferencia.TotalMilliseconds);
                    return vigente;
                }

                _logger.LogInformation(
                    "CUFD vigente descartado (Id:{Id}) porque su FechaEmisionSolicitud ({F1}) "
                    + "difiere {Delta} ms de la fechaEmision actual ({F2}). "
                    + "Se solicitará uno nuevo al SIAT para evitar error 1002/1003.",
                    vigente.Id,
                    vigente.FechaEmisionSolicitud.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    (long)diferencia.TotalMilliseconds,
                    fechaEmisionUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                // Lo marcamos como vencido para que no se reconsidere en esta consulta.
                // No lo eliminamos para conservar trazabilidad histórica.
                vigente.FechaVigencia = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                _logger.LogInformation(
                    "No hay CUFD vigente para PV ({Suc},{PV}). Solicitando al SIAT...",
                    codigoSucursal, codigoPuntoVenta);
            }

            return await SolicitarCufdAsync(codigoSucursal, codigoPuntoVenta, fechaEmision, ct);
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