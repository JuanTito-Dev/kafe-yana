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
    /// Servicio que sincroniza el catálogo paramétrico de unidades de medida
    /// del SIAT (<c>sincronizarParametricaUnidadMedida</c>).
    ///
    /// Espejo de <see cref="SincronizadorCatMetodosPago"/> (sync 11):
    ///   - **MERGE** con la BD (no reemplazo atómico). Razones:
    ///     1. La tabla <c>CatUnidadesMedida</c> tiene un flag <c>Activo</c>
    ///        controlado por el operador. Reemplazar la tabla borraría la
    ///        configuración manual.
    ///     2. El catálogo tiene ~50–100 entradas y cambia poco — el sync es
    ///        diario a las 08:10 BOT (espejo de <c>SincronizadorCatTipoEmision</c>).
    ///   - **Seed default**: los 9 códigos que la cafetería ya usa
    ///        (UNIDAD=57, VASO=97, BOTELLA=5, CAJA=6, MILIGRAMO=33, GRAMO=17,
    ///        LITRO=28, MILILITRO=34, OTRO=62) arrancan <c>Activo=true</c> la
    ///        primera vez. El resto <c>Activo=false</c> hasta que el operador
    ///        los habilite.
    ///   - **Conservador**: si el SIN deja de devolver un código que estaba
    ///        activo, NO se desactiva (preserva la config manual).
    ///
    /// Se ejecuta al boot del server + diario a las 08:10 BOT vía
    /// <c>SincronizacionUnidadMedidaHostedService</c> y bajo demanda manual
    /// (<c>POST /api/catalogos/sincronizar-unidades-medida</c>).
    /// </summary>
    public class SincronizadorCatUnidadMedida
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatUnidadMedida> _logger;

        public SincronizadorCatUnidadMedida(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatUnidadMedida> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de unidades de medida desde el SIAT.
        ///
        /// Estrategia de merge:
        ///   1. Listar TODAS las filas actuales de <c>CatUnidadesMedida</c>
        ///      para preservar el flag <c>Activo</c> existente.
        ///   2. Iterar PVs activos y obtener la primera respuesta exitosa
        ///      del SIN.
        ///   3. Para cada código del SIN:
        ///      - Si NO existe en BD → INSERT con <c>Activo</c> según el seed
        ///        default (los códigos en
        ///        <see cref="UnidadMedidaSiatCatalogo.HardcodedActivosCodes"/>
        ///        → true; resto → false).
        ///      - Si existe en BD → UPDATE solo <c>Descripcion</c> y
        ///        <c>FechaSincronizacion</c>. **NO toca <c>Activo</c>**.
        ///   4. Filas que estaban en BD pero el SIN ya no devuelve → quedan
        ///      como están (preserva auditoría + config operador).
        ///
        /// Devuelve (CantidadTotal, Nuevos, Actualizados, PvsExitosos).
        /// </summary>
        public async Task<(int CantidadTotal, int Nuevos, int Actualizados, int PvsExitosos)> SincronizarAsync(
            CancellationToken ct = default)
        {
            // 1) Snapshot del estado actual de la BD (preservar Activo).
            Dictionary<int, bool> estatusActual;
            await using (var dbLectura = await _dbFactory.CreateDbContextAsync(ct))
            {
                estatusActual = await dbLectura.CatUnidadesMedida
                    .AsNoTracking()
                    .ToDictionaryAsync(u => u.Codigo, u => u.Activo, ct);
            }

            // 2) Listar PVs activos.
            List<PuntoVentaSiat> puntosVenta;
            await using (var dbLectura2 = await _dbFactory.CreateDbContextAsync(ct))
            {
                puntosVenta = await dbLectura2.PuntosVentaSiat
                    .AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.CodigoSucursal)
                    .ThenBy(p => p.CodigoPuntoVenta)
                    .ToListAsync(ct);
            }

            if (puntosVenta.Count == 0)
            {
                _logger.LogWarning(
                    "No hay PuntosVentaSiat activos. CatUnidadesMedida no se sincronizará.");
                return (0, 0, 0, 0);
            }

            // 3) Iterar PVs. Acumular el primer resultado exitoso.
            List<UnidadMedidaSiatDto> unidadesSiat = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaUnidadMedidaAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de unidades de medida para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} unidades de medida",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.Unidades.Count);

                    // Guardamos la primera respuesta exitosa.
                    if (unidadesSiat is null && respuesta.Unidades.Count > 0)
                        unidadesSiat = respuesta.Unidades;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando unidades de medida para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (unidadesSiat is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatUnidadesMedida NO se actualizará.");
                return (0, 0, 0, 0);
            }

            // 4) Merge en BD (no reemplazo).
            var (nuevos, actualizados) = await HacerMergeAsync(unidadesSiat, ct);

            // 5) Refrescar caché estático en memoria con los datos mergeados.
            //    Pasamos el estatusActual actualizado para que la caché refleje
            //    la decisión Activo que acabamos de aplicar a la BD.
            var estatusFinal = await ObtenerEstatusFinalAsync(ct);
            UnidadMedidaSiatCatalogo.Refrescar(
                unidadesSiat.Select(u => (u.Codigo, u.Descripcion)),
                estatusFinal);

            // 6) Marcar UltimaSyncUnidadMedida en los PVs exitosos.
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncUnidadMedida = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatUnidadesMedida OK: {Total} totales del SIN, {Nuevos} nuevos, {Actualizados} actualizados, {PVs} PVs actualizados",
                unidadesSiat.Count, nuevos, actualizados, pvsExitosos.Count);

            return (unidadesSiat.Count, nuevos, actualizados, pvsExitosos.Count);
        }

        /// <summary>
        /// MERGE de los códigos del SIN contra la tabla existente.
        /// </summary>
        private async Task<(int Nuevos, int Actualizados)> HacerMergeAsync(
            List<UnidadMedidaSiatDto> unidadesSiat,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevosCount = 0;
            var actualizadosCount = 0;

            // Traer las filas actuales con tracking.
            var existentes = await db.CatUnidadesMedida
                .ToDictionaryAsync(u => u.Codigo, ct);

            foreach (var u in unidadesSiat)
            {
                if (u.Codigo <= 0 || string.IsNullOrWhiteSpace(u.Descripcion)) continue;

                if (existentes.TryGetValue(u.Codigo, out var fila))
                {
                    // UPDATE solo descripción + fecha. PRESERVAR Activo.
                    var nuevaDesc = u.Descripcion.Trim();
                    if (fila.Descripcion != nuevaDesc)
                    {
                        fila.Descripcion = nuevaDesc;
                        actualizadosCount++;
                    }
                    fila.FechaSincronizacion = ahora;
                }
                else
                {
                    // INSERT nuevo. Activo = seed default.
                    db.CatUnidadesMedida.Add(new CatUnidadMedida
                    {
                        Codigo = u.Codigo,
                        Descripcion = u.Descripcion.Trim(),
                        Activo = UnidadMedidaSiatCatalogo.HardcodedActivosCodes.Contains(u.Codigo),
                        FechaSincronizacion = ahora
                    });
                    nuevosCount++;
                }
            }

            // Filas en BD que el SIN ya no devuelve → se mantienen (no se borran).
            var codigosSiat = new HashSet<int>(
                unidadesSiat.Where(u => u.Codigo > 0).Select(u => u.Codigo));
            var desaparecidos = existentes.Keys.Where(c => !codigosSiat.Contains(c)).ToList();
            if (desaparecidos.Count > 0)
            {
                _logger.LogWarning(
                    "El SIN ya no devuelve {N} códigos de unidad de medida que estaban en BD: {Codigos}. Se mantienen sin modificar.",
                    desaparecidos.Count, string.Join(", ", desaparecidos));
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (nuevosCount, actualizadosCount);
        }

        /// <summary>
        /// Lee el estado final de <c>Activo</c> en BD para refrescar el caché
        /// con la decisión que acabamos de aplicar.
        /// </summary>
        private async Task<Dictionary<int, bool>> ObtenerEstatusFinalAsync(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.CatUnidadesMedida
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Codigo, u => u.Activo, ct);
        }
    }
}