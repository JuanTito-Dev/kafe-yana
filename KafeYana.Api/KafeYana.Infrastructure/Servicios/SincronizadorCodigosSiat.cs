using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.Entities.Facturacion;
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
    /// Servicio que orquesta la sincronización del catálogo de productos/servicios
    /// del SIAT en la tabla local <c>CodigosSiat</c>.
    ///
    /// Espejo de <see cref="SincronizadorCatLeyenda"/>: por NIT, el SIN
    /// devuelve la misma lista de productos para todos los puntos de venta,
    /// así que sincronizamos la tabla una sola vez con la primera respuesta
    /// exitosa.
    ///
    /// Diferencia clave: el SIN devuelve productos para TODAS las actividades
    /// económicas del NIT. Acá filtramos por la actividad económica PRINCIPAL
    /// del operador (la misma que usa <c>ICatActividadResolver.ResolverCaebVigenteAsync</c>)
    /// ANTES de persistir, así la tabla queda específica del CAEB del operador.
    /// Si el operador cambia de actividad y vuelve a sincronizar, la tabla refleja
    /// solo los productos relevantes para la nueva actividad.
    ///
    /// Esta tabla es la que consume el modal <c>CodigoSinModal</c> del frontend
    /// cuando el operador crea/edita un producto del menú y le asigna un código
    /// SIN. Por eso la FK lógica es (CodigoProducto, CodigoActividad).
    /// </summary>
    public class SincronizadorCodigosSiat
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICatActividadResolver _actividadResolver;
        private readonly ILogger<SincronizadorCodigosSiat> _logger;

        public SincronizadorCodigosSiat(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            ICatActividadResolver actividadResolver,
            ILogger<SincronizadorCodigosSiat> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _actividadResolver = actividadResolver;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el catálogo de productos/servicios para el CAEB principal
        /// del operador. Itera los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa para filtrar por actividad principal y
        /// reemplazar la tabla <c>CodigosSiat</c>. Devuelve la cantidad de filas
        /// insertadas y la cantidad de PVs actualizados.
        /// </summary>
        public async Task<(int Cantidad, int PvsExitosos)> SincronizarAsync(CancellationToken ct = default)
        {
            // 1) Resolver CAEB principal. Si CatActividades está vacía, esto
            //    lanza CatalogoNoSincronizadoException — propagamos tal cual.
            var caebPrincipal = await _actividadResolver.ResolverCaebVigenteAsync(ct);
            _logger.LogInformation(
                "Sincronización de productos/servicios filtrada por CAEB principal {Caeb}",
                caebPrincipal);

            // 2) Resolver descripción de la actividad principal para popular
            //    CodigosSiat.DescripcionActividad (el SOAP no la trae, la
            //    tenemos local en CatActividades).
            string descripcionActividadPrincipal;
            await using (var dbLecturaCat = await _dbFactory.CreateDbContextAsync(ct))
            {
                descripcionActividadPrincipal = await dbLecturaCat.CatActividades
                    .AsNoTracking()
                    .Where(a => a.CodigoCaeb == caebPrincipal)
                    .Select(a => a.Descripcion)
                    .FirstOrDefaultAsync(ct) ?? string.Empty;
            }

            // 3) Listar PVs activos
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
                    "No hay PuntosVentaSiat activos. CodigosSiat no se sincronizará.");
                return (0, 0);
            }

            // 4) Iterar PVs. Acumular el primer resultado exitoso para la maestra.
            List<ProductoServicioSiatDto> productosMaestra = null;
            var pvsExitosos = new List<int>();

            foreach (var pv in puntosVenta)
            {
                try
                {
                    var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    var respuesta = await _siat.SincronizarListaProductosServiciosAsync(
                        cuis.Codigo,
                        pv.CodigoSucursal,
                        pv.CodigoPuntoVenta,
                        ct);

                    if (!respuesta.Transaccion)
                    {
                        var errores = string.Join(" | ", respuesta.CodigosRespuesta
                            .Select(c => $"[{c.Codigo}] {c.Descripcion}"));
                        _logger.LogWarning(
                            "SIAT rechazó sincronización de productos/servicios para PV {Nombre} ({Suc},{PV}). Errores: {Errores}",
                            pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta, errores);
                        continue;
                    }

                    _logger.LogInformation(
                        "SIAT OK para PV {Nombre} ({Suc},{PV}): {Cantidad} productos (de todas las actividades)",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta,
                        respuesta.ProductosServicios.Count);

                    // Guardamos la primera respuesta exitosa para la tabla maestra
                    if (productosMaestra is null && respuesta.ProductosServicios.Count > 0)
                        productosMaestra = respuesta.ProductosServicios;

                    pvsExitosos.Add(pv.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sincronizando productos/servicios para PV {Nombre} ({Suc},{PV}). Se continúa con los demás.",
                        pv.Nombre, pv.CodigoSucursal, pv.CodigoPuntoVenta);
                }
            }

            if (productosMaestra is null || pvsExitosos.Count == 0)
            {
                _logger.LogWarning(
                    "Ningún PV devolvió datos. CodigosSiat NO se actualizará.");
                return (0, 0);
            }

            // 5) Reemplazo atómico en la tabla MAESTRA, FILTRANDO por CAEB principal.
            //    El SIAT devuelve productos para muchas actividades; solo conservamos
            //    los del CAEB del operador para que la tabla quede específica.
            var cantidad = await ReemplazarTablaMaestraAsync(
                productosMaestra,
                caebPrincipal,
                descripcionActividadPrincipal,
                ct);

            // 6) Marcar UltimaSyncCodigosSiat para todos los PVs exitosos
            var ahora = DateTime.UtcNow;
            await using (var dbUpdate = await _dbFactory.CreateDbContextAsync(ct))
            {
                var pvsAMarcar = await dbUpdate.PuntosVentaSiat
                    .Where(p => pvsExitosos.Contains(p.Id))
                    .ToListAsync(ct);
                foreach (var pv in pvsAMarcar)
                    pv.UltimaSyncCodigosSiat = ahora;
                await dbUpdate.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Sincronización CodigosSiat OK: {Cantidad} productos para CAEB {Caeb}, {PVs} PVs actualizados",
                cantidad, caebPrincipal, pvsExitosos.Count);

            return (cantidad, pvsExitosos.Count);
        }

        private async Task<int> ReemplazarTablaMaestraAsync(
            List<ProductoServicioSiatDto> productos,
            string caebPrincipal,
            string descripcionActividadPrincipal,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.CodigosSiat.ExecuteDeleteAsync(ct);

            var nuevos = productos
                .Where(p => !string.IsNullOrWhiteSpace(p.CodigoProducto)
                         && !string.IsNullOrWhiteSpace(p.CodigoActividad)
                         && p.CodigoActividad == caebPrincipal
                         && !string.IsNullOrWhiteSpace(p.DescripcionProducto))
                .Select(p => new CodigoSiat
                {
                    CodigoProducto = p.CodigoProducto.Trim(),
                    CodigoActividad = p.CodigoActividad.Trim(),
                    DescripcionProducto = p.DescripcionProducto.Trim(),
                    DescripcionActividad = descripcionActividadPrincipal
                })
                // Dedupe por (CodigoProducto, CodigoActividad) — el SIAT a
                // veces repite el mismo producto en una misma respuesta.
                .GroupBy(c => new { c.CodigoProducto, c.CodigoActividad })
                .Select(g => g.First())
                .ToList();

            if (nuevos.Count > 0)
            {
                db.CodigosSiat.AddRange(nuevos);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return nuevos.Count;
        }
    }
}