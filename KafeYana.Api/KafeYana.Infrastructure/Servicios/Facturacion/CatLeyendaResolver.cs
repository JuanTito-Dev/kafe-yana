using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Implementación de <see cref="ICatLeyendaResolver"/>.
    ///
    /// Lee de <c>CatLeyendas</c> (sincronizada por <c>SincronizadorCatLeyenda</c>)
    /// todas las leyendas que aplican al CAEB indicado y devuelve una al azar.
    /// Si la tabla está vacía para ese CAEB, lanza <see cref="VentaException"/>
    /// con instrucción clara al operador (fail-closed, sin fallback silencioso).
    /// </summary>
    public class CatLeyendaResolver(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<CatLeyendaResolver> logger) : ICatLeyendaResolver
    {
        public async Task<string> ObtenerAleatoriaAsync(string codigoActividad, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigoActividad))
                throw new VentaException("No se puede obtener una leyenda sin CAEB.");

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var descripciones = await db.CatLeyendas
                .AsNoTracking()
                .Where(l => l.CodigoActividad == codigoActividad)
                .Select(l => l.DescripcionLeyenda)
                .ToListAsync(ct);

            if (descripciones.Count == 0)
            {
                logger.LogWarning(
                    "No hay leyendas sincronizadas para CAEB {Caeb}. "
                    + "Ejecute POST /api/catalogos/sincronizar-leyendas.",
                    codigoActividad);

                throw new VentaException(
                    $"No hay leyendas sincronizadas para la actividad '{codigoActividad}'. "
                    + "Ejecute POST /api/catalogos/sincronizar-leyendas.");
            }

            return descripciones[Random.Shared.Next(descripciones.Count)];
        }
    }
}
