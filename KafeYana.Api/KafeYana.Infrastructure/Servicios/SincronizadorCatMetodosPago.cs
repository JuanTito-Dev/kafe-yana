using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio que sincroniza el catálogo paramétrico de tipos de método de
    /// pago del SIAT (<c>sincronizarParametricaTipoMetodoPago</c>).
    ///
    /// DIFERENCIAS con los sincronizadores anteriores (1..10):
    ///   - **MERGE** con la BD (no reemplazo atómico). Razones:
    ///     1. La tabla <c>CatMetodosPago</c> tiene un flag <c>Activo</c>
    ///        controlado por el operador. Reemplazar la tabla borraría la
    ///        configuración manual.
    ///     2. El catálogo tiene ~308 entradas y cambia poco — no aporta un
    ///        sync diario (decisión confirmada).
    ///   - **Seed default**: los códigos 1=EFECTIVO, 2=TARJETA y
    ///        7=TRANSFERENCIA arrancan <c>Activo=true</c> la primera vez.
    ///        El resto <c>Activo=false</c> hasta que el operador los habilite.
    ///   - **Conservador**: si el SIN deja de devolver un código que estaba
    ///        activo, NO se desactiva (preserva la config manual).
    ///
    /// Se ejecuta al boot del server (vía <c>SincronizacionMetodosPagoHostedService</c>)
    /// y bajo demanda manual (<c>POST /api/catalogos/sincronizar-metodos-pago</c>).
    /// </summary>
    public class SincronizadorCatMetodosPago
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatMetodosPago> _logger;

        public SincronizadorCatMetodosPago(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatMetodosPago> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de métodos de pago desde el SIAT.
        ///
        /// Estrategia de merge:
        ///   1. Listar TODAS las filas actuales de <c>CatMetodosPago</c> para
        ///      preservar el flag <c>Activo</c> existente.
        ///   2. Iterar PVs activos y obtener la primera respuesta exitosa
        ///      del SIN.
        ///   3. Para cada código del SIN:
        ///      - Si NO existe en BD → INSERT con <c>Activo</c> según el seed
        ///        default (1=EFECTIVO, 2=TARJETA, 7=TRANSFERENCIA → true; resto → false).
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
                estatusActual = await dbLectura.CatMetodosPago
                    .AsNoTracking()
                    .ToDictionaryAsync(m => m.Codigo, m => m.Activo, ct);
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
                    "No hay PuntosVentaSiat activos. CatMetodosPago no se sincronizará.");
                return (0, 0, 0, 0);
            }

            // 3) Iterar PVs. Acumular el primer resultado exitoso.
            List<TipoMetodoPagoSiatDto> metodosSiat = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaTipoMetodoPagoAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de métodos de pago para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} métodos de pago",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.MetodosPago.Count);

                    // Guardamos la primera respuesta exitosa.
                    if (metodosSiat is null && respuesta.MetodosPago.Count > 0)
                        metodosSiat = respuesta.MetodosPago;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando métodos de pago para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (metodosSiat is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatMetodosPago NO se actualizará.");
                return (0, 0, 0, 0);
            }

            // 4) Merge en BD (no reemplazo).
            var (nuevos, actualizados) = await HacerMergeAsync(metodosSiat, estatusActual, ct);

            // 5) Refrescar caché estático en memoria con los datos mergeados.
            //    Pasamos el estatusActual actualizado para que la caché refleje
            //    la decisión Activo que acabamos de aplicar a la BD.
            var estatusFinal = await ObtenerEstatusFinalAsync(ct);
            MetodoPagoSiatCatalogo.Refrescar(
                metodosSiat.Select(m => (m.Codigo, m.Descripcion)),
                estatusFinal);

            // 6) Marcar UltimaSyncMetodoPago en los PVs exitosos.
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncMetodoPago = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatMetodosPago OK: {Total} totales del SIN, {Nuevos} nuevos, {Actualizados} actualizados, {PVs} PVs actualizados",
                metodosSiat.Count, nuevos, actualizados, pvsExitosos.Count);

            return (metodosSiat.Count, nuevos, actualizados, pvsExitosos.Count);
        }

        /// <summary>
        /// MERGE de los códigos del SIN contra la tabla existente.
        /// </summary>
        private async Task<(int Nuevos, int Actualizados)> HacerMergeAsync(
            List<TipoMetodoPagoSiatDto> metodosSiat,
            Dictionary<int, bool> estatusActual,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevosCount = 0;
            var actualizadosCount = 0;

            // Traer las filas actuales con tracking.
            var existentes = await db.CatMetodosPago
                .ToDictionaryAsync(m => m.Codigo, ct);

            // Seed default: 1=EFECTIVO, 2=TARJETA y 7=TRANSFERENCIA arrancan activos.
            bool SeedActivo(int codigo) => codigo == 1 || codigo == 2 || codigo == 7;

            foreach (var m in metodosSiat)
            {
                if (m.Codigo <= 0 || string.IsNullOrWhiteSpace(m.Descripcion)) continue;

                if (existentes.TryGetValue(m.Codigo, out var fila))
                {
                    // UPDATE solo descripción + fecha. PRESERVAR Activo.
                    var nuevaDesc = m.Descripcion.Trim();
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
                    db.CatMetodosPago.Add(new CatTipoMetodoPago
                    {
                        Codigo = m.Codigo,
                        Descripcion = m.Descripcion.Trim(),
                        Activo = SeedActivo(m.Codigo),
                        FechaSincronizacion = ahora
                    });
                    nuevosCount++;
                }
            }

            // Filas en BD que el SIN ya no devuelve → se mantienen (no se borran).
            var codigosSiat = new HashSet<int>(
                metodosSiat.Where(m => m.Codigo > 0).Select(m => m.Codigo));
            var desaparecidos = existentes.Keys.Where(c => !codigosSiat.Contains(c)).ToList();
            if (desaparecidos.Count > 0)
            {
                _logger.LogWarning(
                    "El SIN ya no devuelve {N} códigos de método de pago que estaban en BD: {Codigos}. Se mantienen sin modificar.",
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
            return await db.CatMetodosPago
                .AsNoTracking()
                .ToDictionaryAsync(m => m.Codigo, m => m.Activo, ct);
        }
    }
}