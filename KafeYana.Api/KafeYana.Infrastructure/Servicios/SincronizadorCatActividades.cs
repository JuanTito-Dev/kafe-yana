using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio singleton que orquesta la sincronización del catálogo
    /// de actividades económicas (CAEB) contra el SIAT.
    ///
    /// Estrategia multi-punto-de-venta:
    ///   1) Lee todos los PuntosVentaSiat activos.
    ///   2) Para cada uno: obtiene CUIS vigente y llama al SOAP de sincronización.
    ///   3) Como el SIN devuelve la misma lista por NIT, se sincroniza la tabla
    ///      MAESTRA CatActividades UNA SOLA VEZ con la primera respuesta exitosa.
    ///   4) Actualiza UltimaSyncActividades de CADA PV procesado.
    ///
    /// Si un PV falla (CUIS vencido, error de red, etc.), se loguea y se continúa
    /// con los siguientes.
    /// </summary>
    public class SincronizadorCatActividades
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatActividades> _logger;

        public SincronizadorCatActividades(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatActividades> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de actividades para todos los PuntosVentaSiat activos.
        /// Devuelve la cantidad total de filas insertadas en la tabla maestra.
        /// </summary>
        public async Task<int> SincronizarAsync(CancellationToken ct = default)
        {
            // 1) Listar PVs activos
            List<PuntoVentaSiat> puntosVenta;
            await using (var dbLectura = await _dbFactory.CreateDbContextAsync(ct))
            {
                puntosVenta = await dbLectura.PuntosVentaSiat
                    .AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.CodigoSucursal)
                    .ThenBy(p => p.CodigoPuntoVenta)
                    .ToListAsync(ct);
            }

            if (puntosVenta.Count == 0)
            {
                _logger.LogWarning(
                    "No hay PuntosVentaSiat activos. CatActividades no se sincronizará.");
                return 0;
            }

            // 2) Iterar cada PV. Acumular el primer resultado exitoso para la maestra.
            List<ActividadSiatDto> actividadesMaestra = null;
            var pvsExitosos = new List<int>(); // Ids de PuntoVentaSiat que devolvieron OK

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarActividadesAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} actividades",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.Actividades.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (actividadesMaestra is null && respuesta.Actividades.Count > 0)
                        actividadesMaestra = respuesta.Actividades;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando actividades para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (actividadesMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatActividades NO se actualizará.");
                return 0;
            }

            // 3) Upsert atómico en la tabla MAESTRA
            var cantidad = await ReemplazarTablaMaestraAsync(actividadesMaestra, ct);

            // 4) Marcar UltimaSyncActividades para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncActividades = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatActividades OK: {Cantidad} actividades, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return cantidad;
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<ActividadSiatDto> actividades,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // EF genera el SQL con identificadores entrecomillados (preserva mayúsculas).
            await db.CatActividades.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevas = actividades
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
            return nuevas.Count;
        }
    }
}