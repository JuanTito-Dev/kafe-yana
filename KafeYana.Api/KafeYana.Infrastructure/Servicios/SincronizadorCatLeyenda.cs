using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio que orquesta la sincronización del catálogo de leyendas
    /// obligatorias del SIAT en la BD local.
    ///
    /// Espejo de <see cref="SincronizadorCatMotivoAnulacion"/>: por NIT, el SIN
    /// devuelve la misma lista de leyendas para todos los puntos de venta,
    /// así que sincronizamos la tabla MAESTRA <c>CatLeyendas</c> una sola vez
    /// con la primera respuesta exitosa.
    ///
    /// Diferencia clave: el SIN devuelve leyendas para TODAS las actividades
    /// económicas del NIT (4630600, 5610200, 6920000, 7020110, etc.). Acá
    /// filtramos por la actividad económica PRINCIPAL del operador (la misma
    /// que usa <c>ICatActividadResolver.ResolverCaebVigenteAsync</c>) ANTES
    /// de persistir, así la tabla queda chica y específica del CAEB del
    /// operador. Si el operador cambia de actividad y vuelve a sincronizar,
    /// la tabla refleja solo las leyendas relevantes para la nueva actividad.
    /// </summary>
    public class SincronizadorCatLeyenda
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICatActividadResolver _actividadResolver;
        private readonly ILogger<SincronizadorCatLeyenda> _logger;

        public SincronizadorCatLeyenda(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ICatActividadResolver actividadResolver,
            ILogger<SincronizadorCatLeyenda> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _actividadResolver = actividadResolver;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de leyendas para el CAEB principal del
        /// operador. Itera los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa para filtrar por actividad principal y
        /// reemplazar la tabla maestra. Devuelve la cantidad de filas
        /// insertadas y la cantidad de PVs actualizados.
        /// </summary>
        public async Task<(int Cantidad, int PvsExitosos)> SincronizarAsync(CancellationToken ct = default)
        {
            // 1) Resolver CAEB principal. Si CatActividades está vacía, esto
            //    lanza CatalogoNoSincronizadoException — propagamos tal cual.
            var caebPrincipal = await _actividadResolver.ResolverCaebVigenteAsync(ct);
            _logger.LogInformation(
                "Sincronización de leyendas filtrada por CAEB principal {Caeb}",
                caebPrincipal);

            // 2) Listar PVs activos
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
                    "No hay PuntosVentaSiat activos. CatLeyendas no se sincronizará.");
                return (0, 0);
            }

            // 3) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<LeyendaSiatDto> leyendasMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarListaLeyendasFacturaAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de leyendas para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} leyendas (de todas las actividades)",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.Leyendas.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (leyendasMaestra is null && respuesta.Leyendas.Count > 0)
                        leyendasMaestra = respuesta.Leyendas;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando leyendas para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (leyendasMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatLeyendas NO se actualizará.");
                return (0, 0);
            }

            // 4) Reemplazo atómico en la tabla MAESTRA, FILTRANDO por CAEB principal.
            //    El SIAT devuelve leyendas para muchas actividades; solo conservamos
            //    las del CAEB del operador para que la tabla quede específica.
            var cantidad = await ReemplazarTablaMaestraAsync(leyendasMaestra, caebPrincipal, ct);

            // 5) Marcar UltimaSyncLeyendas para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncLeyendas = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatLeyendas OK: {Cantidad} leyendas para CAEB {Caeb}, {PVs} PVs actualizados",
                cantidad, caebPrincipal, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<LeyendaSiatDto> leyendas,
            string caebPrincipal,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.CatLeyendas.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevos = leyendas
                .Where(l => !string.IsNullOrWhiteSpace(l.CodigoActividad)
                         && l.CodigoActividad == caebPrincipal
                         && !string.IsNullOrWhiteSpace(l.DescripcionLeyenda))
                .Select(l => new CatLeyenda
                {
                    CodigoActividad = l.CodigoActividad.Trim(),
                    DescripcionLeyenda = l.DescripcionLeyenda.Trim(),
                    FechaSincronizacion = ahora
                })
                // Dedupe por (CodigoActividad, DescripcionLeyenda) — el SIAT a
                // veces repite la misma leyenda en una misma respuesta.
                .GroupBy(l => new { l.CodigoActividad, l.DescripcionLeyenda })
                .Select(g => g.First())
                .ToList();

            if (nuevos.Count > 0)
            {
                db.CatLeyendas.AddRange(nuevos);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevos.Count;
        }
    }
}
