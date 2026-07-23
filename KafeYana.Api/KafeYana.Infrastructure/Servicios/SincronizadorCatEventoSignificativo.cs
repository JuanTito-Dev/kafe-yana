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
    /// Servicio que orquesta la sincronización del catálogo paramétrico de
    /// eventos significativos del SIAT en la tabla local <c>CatEventosSignificativos</c>.
    ///
    /// Espejo de <see cref="SincronizadorCodigosSiat"/> y
    /// <see cref="SincronizadorCatLeyenda"/>: por NIT, el SIN devuelve la misma
    /// lista para todos los puntos de venta, así que sincronizamos la tabla
    /// una sola vez con la primera respuesta exitosa.
    ///
    /// Diferencia clave: este catálogo NO se filtra por actividad económica.
    /// Los 7 eventos significativos del SIN aplican universalmente a todos los
    /// contribuyentes, así que la tabla queda con exactamente las filas que el
    /// SIN devuelve (típicamente 7).
    ///
    /// Fuera de scope: el uso de este catálogo para facturación en contingencia
    /// (registro del evento, buffer offline, paquete) se implementa en un
    /// ticket separado.
    /// </summary>
    public class SincronizadorCatEventoSignificativo
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatEventoSignificativo> _logger;

        public SincronizadorCatEventoSignificativo(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatEventoSignificativo> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de eventos significativos desde el SIAT.
        /// Itera los PuntosVentaSiat activos, usa la primera respuesta exitosa
        /// para reemplazar la tabla <c>CatEventosSignificativos</c>. Devuelve
        /// la cantidad de filas insertadas y la cantidad de PVs actualizados.
        /// </summary>
        public async Task<(int Cantidad, int PvsExitosos)> SincronizarAsync(CancellationToken ct = default)
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
                    "No hay PuntosVentaSiat activos. CatEventosSignificativos no se sincronizará.");
                return (0, 0);
            }

            // 2) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<EventoSignificativoSiatDto> eventosMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaEventosSignificativosAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de eventos significativos para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} eventos significativos",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.EventosSignificativos.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (eventosMaestra is null && respuesta.EventosSignificativos.Count > 0)
                        eventosMaestra = respuesta.EventosSignificativos;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando eventos significativos para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (eventosMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatEventosSignificativos NO se actualizará.");
                return (0, 0);
            }

            // 3) Reemplazo atómico en la tabla MAESTRA (sin filtro por CAEB:
            //    el catálogo de eventos significativos es universal).
            var cantidad = await ReemplazarTablaMaestraAsync(eventosMaestra, ct);

            // 4) Marcar UltimaSyncEventosSignificativos para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncEventosSignificativos = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatEventosSignificativos OK: {Cantidad} eventos, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<EventoSignificativoSiatDto> eventos,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.CatEventosSignificativos.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevos = eventos
                .Where(e => e.Codigo > 0 && !string.IsNullOrWhiteSpace(e.Descripcion))
                .Select(e => new CatEventoSignificativo
                {
                    Codigo = e.Codigo,
                    Descripcion = e.Descripcion.Trim(),
                    FechaSincronizacion = ahora
                })
                // Dedupe por Codigo — el SIAT a veces repite el mismo evento
                // en una misma respuesta.
                .GroupBy(c => c.Codigo)
                .Select(g => g.First())
                .OrderBy(c => c.Codigo)
                .ToList();

            if (nuevos.Count > 0)
            {
                db.CatEventosSignificativos.AddRange(nuevos);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevos.Count;
        }
    }
}
