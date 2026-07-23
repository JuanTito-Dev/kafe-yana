using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio que orquesta la sincronización del catálogo paramétrico de
    /// tipos de documento de identidad contra el SIAT.
    ///
    /// Espejo híbrido:
    ///   - Estructura de <see cref="SincronizadorCatPaisOrigen"/>: catálogo
    ///     UNIVERSAL (no se filtra por CAEB), reemplazo atómico de la tabla
    ///     maestra <c>CatTiposDocumentoIdentidad</c>.
    ///   - Refresco de caché estático en memoria (como
    ///     <see cref="SincronizadorCatMotivoAnulacion"/>) vía
    ///     <see cref="TipoDocumentoIdentidadSiatCatalogo.Refrescar"/> para que
    ///     las validaciones de <c>codigoTipoDocumentoIdentidad</c> en cada
    ///     venta vean inmediatamente las descripciones oficiales del SIN.
    /// </summary>
    public class SincronizadorCatTipoDocumentoIdentidad
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatTipoDocumentoIdentidad> _logger;

        public SincronizadorCatTipoDocumentoIdentidad(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatTipoDocumentoIdentidad> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de tipos de documento de identidad desde el SIAT.
        /// Itera los PuntosVentaSiat activos, usa la primera respuesta exitosa
        /// para reemplazar la tabla maestra <c>CatTiposDocumentoIdentidad</c>
        /// y refresca el caché estático usado por las validaciones.
        /// Devuelve la cantidad de filas insertadas y la cantidad de PVs
        /// actualizados.
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
                    "No hay PuntosVentaSiat activos. CatTiposDocumentoIdentidad no se sincronizará.");
                return (0, 0);
            }

            // 2) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<TipoDocumentoIdentidadSiatDto> tiposMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaTipoDocumentoIdentidadAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de tipos de documento de identidad para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} tipos de documento de identidad",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.TiposDocumentoIdentidad.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (tiposMaestra is null && respuesta.TiposDocumentoIdentidad.Count > 0)
                        tiposMaestra = respuesta.TiposDocumentoIdentidad;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando tipos de documento de identidad para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (tiposMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatTiposDocumentoIdentidad NO se actualizará.");
                return (0, 0);
            }

            // 3) Reemplazo atómico en la tabla MAESTRA (sin filtro por CAEB:
            //    el catálogo de tipos de documento de identidad es universal).
            var cantidad = await ReemplazarTablaMaestraAsync(tiposMaestra, ct);

            // 4) Refrescar caché en memoria para que las validaciones vean
            //    las descripciones oficiales del SIN de inmediato.
            TipoDocumentoIdentidadSiatCatalogo.Refrescar(
                tiposMaestra.Select(t => (t.Codigo, t.Descripcion)));

            // 5) Marcar UltimaSyncTipoDocumentoIdentidad para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncTipoDocumentoIdentidad = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatTiposDocumentoIdentidad OK: {Cantidad} tipos, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        /// <summary>
        /// Sirve el catálogo desde <c>CatTiposDocumentoIdentidad</c> cuando el SIAT
        /// no respondió (<see cref="SincronizarAsync"/> devolvió 0 PVs exitosos).
        /// Usa la última sync exitosa persistida, sin límite de antigüedad: los códigos
        /// de tipo de documento del SIN cambian muy rara vez. Si la tabla está vacía
        /// (instalación nueva, nunca sincronizó), no hace nada y el caché sigue en
        /// <c>FallbackHardcoded</c>.
        /// </summary>
        public async Task<bool> IntentarCargarDesdeBaseDatosAsync(CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var filas = await db.CatTiposDocumentoIdentidad
                .AsNoTracking()
                .Select(c => new { c.Codigo, c.Descripcion })
                .ToListAsync(ct);

            if (filas.Count == 0)
            {
                _logger.LogWarning(
                    "CatTiposDocumentoIdentidad está vacía. Se mantiene el fallback hardcodeado.");
                return false;
            }

            TipoDocumentoIdentidadSiatCatalogo.CargarDesdeBaseDatos(
                filas.Select(f => (f.Codigo, f.Descripcion)));

            _logger.LogInformation(
                "SIAT no respondió: se sirvió CatTiposDocumentoIdentidad desde BD ({Cantidad} tipos).",
                filas.Count);
            return true;
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<TipoDocumentoIdentidadSiatDto> tipos,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.CatTiposDocumentoIdentidad.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevos = tipos
                .Where(t => t.Codigo > 0 && !string.IsNullOrWhiteSpace(t.Descripcion))
                .GroupBy(t => t.Codigo)
                .Select(g => g.First())
                .Select(t => new CatTipoDocumentoIdentidad
                {
                    Codigo = t.Codigo,
                    Descripcion = t.Descripcion.Trim(),
                    FechaSincronizacion = ahora
                })
                .ToList();

            if (nuevos.Count > 0)
            {
                db.CatTiposDocumentoIdentidad.AddRange(nuevos);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevos.Count;
        }
    }
}