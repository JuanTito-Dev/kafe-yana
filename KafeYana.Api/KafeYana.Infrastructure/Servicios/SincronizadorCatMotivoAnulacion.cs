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
    /// motivos de anulación contra el SIAT.
    ///
    /// Espejo de <see cref="SincronizadorCatActividades"/>: por NIT, el SIN
    /// devuelve la misma lista de motivos para todos los puntos de venta, así
    /// que sincronizamos la tabla MAESTRA <c>CatMotivosAnulacion</c> una sola
    /// vez con la primera respuesta exitosa y solo auditamos
    /// <c>UltimaSyncMotivoAnulacion</c> en los PVs que respondieron OK.
    ///
    /// Tras persistir la tabla, se llama a
    /// <see cref="MotivoAnulacionSiatCatalogo.Refrescar"/> para que el caché
    /// en memoria usado por las validaciones de anulación (facturas y notas)
    /// vea inmediatamente las descripciones oficiales del SIN.
    /// </summary>
    public class SincronizadorCatMotivoAnulacion
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatMotivoAnulacion> _logger;

        public SincronizadorCatMotivoAnulacion(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatMotivoAnulacion> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de motivos de anulación para todos los
        /// PuntosVentaSiat activos. Devuelve la cantidad de filas insertadas
        /// y la cantidad de PVs actualizados.
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
                    "No hay PuntosVentaSiat activos. CatMotivosAnulacion no se sincronizará.");
                return (0, 0);
            }

            // 2) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<MotivoAnulacionSiatDto> motivosMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarParametricaMotivoAnulacionAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de motivos para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} motivos",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.Motivos.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (motivosMaestra is null && respuesta.Motivos.Count > 0)
                        motivosMaestra = respuesta.Motivos;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando motivos de anulación para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (motivosMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatMotivosAnulacion NO se actualizará.");
                return (0, 0);
            }

            // 3) Upsert atómico en la tabla MAESTRA
            var cantidad = await ReemplazarTablaMaestraAsync(motivosMaestra, ct);

            // 4) Refrescar caché en memoria para que las validaciones vean
            //    las descripciones oficiales del SIN de inmediato.
            MotivoAnulacionSiatCatalogo.Refrescar(
                motivosMaestra.Select(m => (m.Codigo, m.Descripcion)));

            // 5) Marcar UltimaSyncMotivoAnulacion para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncMotivoAnulacion = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CatMotivosAnulacion OK: {Cantidad} motivos, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<MotivoAnulacionSiatDto> motivos,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.CatMotivosAnulacion.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevos = motivos
                .Where(m => m.Codigo > 0 && !string.IsNullOrWhiteSpace(m.Descripcion))
                .Select(m => new CatMotivoAnulacion
                {
                    Codigo = m.Codigo,
                    Descripcion = m.Descripcion.Trim(),
                    FechaSincronizacion = ahora
                })
                .ToList();

            if (nuevos.Count > 0)
            {
                db.CatMotivosAnulacion.AddRange(nuevos);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevos.Count;
        }
    }
}