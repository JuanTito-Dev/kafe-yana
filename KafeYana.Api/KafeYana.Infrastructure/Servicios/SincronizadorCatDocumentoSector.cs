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
    /// de documentos sectoriales contra el SIAT.
    ///
    /// El SIAT devuelve la misma lista por NIT para todos los puntos de venta.
    /// Se itera cada PV activo, se toma la primera respuesta exitosa y se
    /// reemplaza la tabla maestra CatDocumentosSector atómicamente
    /// (DELETE ALL + INSERT ALL en una transacción).
    ///
    /// Si un PV falla o el SIAT devuelve transaccion=false, se loguea y se
    /// continúa con el siguiente (un fallo en un PV no bloquea la sync).
    /// </summary>
    public class SincronizadorCatDocumentoSector
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SincronizadorCatDocumentoSector> _logger;

        public SincronizadorCatDocumentoSector(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SincronizadorCatDocumentoSector> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de documentos sectoriales para todos los PVs activos.
        /// Devuelve la cantidad de filas insertadas en la tabla maestra.
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
                    "No hay PuntosVentaSiat activos. CatDocumentosSector no se sincronizará.");
                return 0;
            }

            // 2) Iterar cada PV. Acumular el primer resultado exitoso para la maestra.
            List<DocumentoSectorSiatDto> documentosMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarDocumentosSectorAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de documentos sector para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} documentos sector",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.DocumentosSector.Count);

                    if (documentosMaestra is null && respuesta.DocumentosSector.Count > 0)
                        documentosMaestra = respuesta.DocumentosSector;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando documentos sector para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (documentosMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CatDocumentosSector NO se actualizará.");
                return 0;
            }

            // 3) Upsert atómico de la tabla MAESTRA
            var cantidad = await ReemplazarTablaMaestraAsync(documentosMaestra, ct);

            _logger.LogInformation(
                "Sincronización CatDocumentosSector OK: {Cantidad} documentos, {PVs} PVs actualizados",
                cantidad, pvsExitosos.Count);

            return cantidad;
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<DocumentoSectorSiatDto> documentos,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // EF genera el SQL con identificadores entrecomillados (preserva mayúsculas).
            await db.CatDocumentosSector.ExecuteDeleteAsync(ct);

            var ahora = DateTime.UtcNow;
            var nuevas = documentos
                .Where(d => d.CodigoClasificador > 0)
                .Select(d => new CatDocumentoSector
                {
                    CodigoClasificador = d.CodigoClasificador,
                    Descripcion = (d.Descripcion ?? string.Empty).Trim(),
                    FechaSincronizacion = ahora
                })
                .ToList();

            if (nuevas.Count > 0)
            {
                db.CatDocumentosSector.AddRange(nuevas);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevas.Count;
        }
    }
}