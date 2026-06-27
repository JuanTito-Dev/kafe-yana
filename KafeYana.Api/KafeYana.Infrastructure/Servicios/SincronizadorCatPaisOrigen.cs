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
    /// Servicio que orquesta la sincronización del catálogo paramétrico de países
    /// de origen del SIAT en la tabla local <c>CatPaisesOrigen</c>.
    ///
    /// Espejo de <see cref="SincronizadorCatEventoSignificativo"/> y
    /// <see cref="SincronizadorCodigosSiat"/>: por NIT, el SIN devuelve la misma
    /// lista para todos los puntos de venta, así que sincronizamos la tabla una
    /// sola vez con la primera respuesta exitosa.
    ///
    /// Diferencia clave: este catálogo NO se filtra por actividad económica.
    /// Los ~211 países del SIN aplican universalmente a todos los contribuyentes.
    ///
    /// Fuera de scope: el uso de este catálogo para Factura Comercial de
    /// Exportación (Documento Sector 3) o para clientes extranjeros con
    /// Pasaporte/CIE se implementa en tickets separados.
    /// </summary>
    public class SincronizadorCatPaisOrigen
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatPaisOrigen> _logger;

        public SincronizadorCatPaisOrigen(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatPaisOrigen> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de países de origen desde el SIAT.
        /// Itera los PuntosVentaSiat activos, usa la primera respuesta exitosa
        /// para reemplazar la tabla <c>CatPaisesOrigen</c>. Devuelve la cantidad
        /// de filas insertadas y la cantidad de PVs actualizados.
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
                    "No hay PuntosVentaSiat activos. CatPaisesOrigen no se sincronizará.");
                return (0, 0);
            }

            // 2) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<PaisOrigenSiatDto> paisesMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaPaisOrigenAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de países de origen para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} países de origen",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.PaisesOrigen.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (paisesMaestra is null && respuesta.PaisesOrigen.Count > 0)
                        paisesMaestra = respuesta.PaisesOrigen;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando países de origen para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (paisesMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatPaisesOrigen NO se actualizará.");
                return (0, 0);
            }

            // 3) Reemplazo atómico en la tabla MAESTRA (sin filtro por CAEB:
            //    el catálogo de países es universal).
            var cantidad = await ReemplazarTablaMaestraAsync(paisesMaestra, ct);

            // 4) Marcar UltimaSyncPaisOrigen para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncPaisOrigen = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatPaisesOrigen OK: {Cantidad} países, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<PaisOrigenSiatDto> paises,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // UPSERT en lugar de DELETE+INSERT. Razón: la FK de
            // `Clientes.IdPaisOrigen` referencia `CatPaisesOrigen.Id` con
            // `OnDelete(DeleteBehavior.Restrict)`. Si hay un cliente extranjero
            // persistido, `ExecuteDeleteAsync` masivo tira
            // "violates RESTRICT setting of foreign key constraint" y deja la
            // transacción rota hasta que se borre el cliente a mano.
            //
            // UPSERT preserva los `Id` existentes, así que el FK sigue
            // apuntando al mismo row después del sync. El SIN no remueve
            // países de la lista (211 estable desde siempre); si la
            // descripción de un país cambia, se actualiza in-place.
            var ahora = DateTime.UtcNow;

            // 1) Dedupe + filtrar la respuesta del SIN (mismo criterio que antes).
            var entrantes = paises
                .Where(p => p.Codigo > 0 && !string.IsNullOrWhiteSpace(p.Descripcion))
                .GroupBy(p => p.Codigo)
                .Select(g => g.First())
                .ToList();

            // 2) Cargar existentes como dict por código (1 sola query).
            var existentes = await db.CatPaisesOrigen
                .ToDictionaryAsync(p => p.Codigo, ct);

            var insertados = 0;
            var actualizados = 0;
            foreach (var p in entrantes)
            {
                if (existentes.TryGetValue(p.Codigo, out var row))
                {
                    // Update in-place: preserva Id → FK de Clientes.IdPaisOrigen
                    // queda apuntando al mismo row.
                    var desc = p.Descripcion.Trim();
                    if (row.Descripcion != desc)
                        row.Descripcion = desc;
                    row.FechaSincronizacion = ahora;
                    actualizados++;
                }
                else
                {
                    db.CatPaisesOrigen.Add(new CatPaisOrigen
                    {
                        Codigo = p.Codigo,
                        Descripcion = p.Descripcion.Trim(),
                        FechaSincronizacion = ahora,
                    });
                    insertados++;
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "CatPaisesOrigen UPSERT: {Insertados} nuevos, {Actualizados} actualizados (total: {Total})",
                insertados, actualizados, existentes.Count + insertados);

            return existentes.Count + insertados;
        }
    }
}