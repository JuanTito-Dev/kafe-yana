using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio singleton que orquesta la sincronización del catálogo
    /// de actividades económicas (CAEB) contra el SIAT.
    ///
    /// Estrategia: DELETE ALL + INSERT ALL dentro de una transacción EF
    /// para garantizar que la tabla siempre quede consistente.
    /// </summary>
    public class SincronizadorCatActividades
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly SiatOptions _opts;
        private readonly ILogger<SincronizadorCatActividades> _logger;

        public SincronizadorCatActividades(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            IOptions<SiatOptions> opts,
            ILogger<SincronizadorCatActividades> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _opts = opts.Value;
            _logger = logger;
        }

        /// <summary>
        /// Llama al SIAT para obtener la lista vigente de actividades y reemplaza
        /// la tabla CatActividades. Devuelve la cantidad de filas insertadas.
        /// </summary>
        public async Task<int> SincronizarAsync(CancellationToken ct = default)
        {
            // 1) Necesitamos un CUIS vigente para hablar con /FacturacionSincronizacion
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                _opts.CodigoSucursal,
                _opts.CodigoPuntoVenta,
                ct);

            // 2) Llamar SOAP al servicio de sincronización
            var respuesta = await _siat.SincronizarActividadesAsync(
                cuis.Codigo,
                _opts.CodigoSucursal,
                _opts.CodigoPuntoVenta,
                ct);

            if (!respuesta.Transaccion)
            {
                var errores = string.Join(" | ", respuesta.CodigosRespuesta
                    .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                _logger.LogError(
                    "SIAT rechazó sincronización de actividades. Errores: {Errores}",
                    errores);
                throw new InvalidOperationException(
                    $"SIAT rechazó sincronizarActividades: {errores}");
            }

            _logger.LogInformation(
                "SIAT devolvió {Cantidad} actividades (transaccion={Transaccion})",
                respuesta.Actividades.Count,
                respuesta.Transaccion);

            // 3) Upsert en transacción (DELETE ALL + INSERT ALL)
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // EF Core genera el SQL con identificadores entrecomillados (preserva
            // mayúsculas). ExecuteSqlRawAsync NO lo hace, lo que rompe contra
            // tablas con nombres PascalCase en PostgreSQL.
            await db.CatActividades.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevas = respuesta.Actividades
                .Where(a => !string.IsNullOrWhiteSpace(a.CodigoCaeb))
                .Select(a => new CatActividad
                {
                    CodigoCaeb = a.CodigoCaeb.Trim(),
                    Descripcion = (a.Descripcion ?? string.Empty).Trim(),
                    TipoActividad = (a.TipoActividad ?? string.Empty).Trim(),
                    FechaSincronizacion = ahora
                })
                .ToList();

            if (nuevas.Count > 0)
            {
                db.CatActividades.AddRange(nuevas);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Sincronización CatActividades OK: {Cantidad} actividades actualizadas (FechaSync: {Fecha:o})",
                nuevas.Count,
                ahora);

            return nuevas.Count;
        }
    }
}