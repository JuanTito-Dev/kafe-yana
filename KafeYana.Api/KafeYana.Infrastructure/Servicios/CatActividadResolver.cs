using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Implementación de <see cref="ICatActividadResolver"/>.
    /// Replica exactamente la lógica que vivía en <c>VentaServices.ResolverActividadEconomica()</c>
    /// (cadena de 3 pasos + throw de tabla vacía), ahora compartida entre
    /// <c>VentaServices</c> y los preparers SIAT.
    /// </summary>
    public class CatActividadResolver(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<DatosEmpresaOptions> empresaOpts,
        ILogger<CatActividadResolver> logger) : ICatActividadResolver
    {
        private readonly DatosEmpresaOptions _empresa = empresaOpts.Value;

        public async Task<string> ResolverCaebVigenteAsync(CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // 1) Preferir la Principal marcada por el SIN (TipoActividad == "P")
            var principal = await db.CatActividades
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.TipoActividad == "P", ct);
            if (principal is not null)
                return principal.CodigoCaeb;

            // 2) Si el SIN no devolvió ninguna como Principal, usar la del appsettings
            var codigoConfig = _empresa.CodigoActividad;
            if (!string.IsNullOrWhiteSpace(codigoConfig))
            {
                var delConfig = await db.CatActividades
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.CodigoCaeb == codigoConfig, ct);
                if (delConfig is not null)
                    return delConfig.CodigoCaeb;
            }

            // 3) Tabla vacía → exigir sincronización (contrato existente preservado)
            var tieneAlgo = await db.CatActividades.AsNoTracking().AnyAsync(ct);
            if (!tieneAlgo)
                throw new CatalogoNoSincronizadoException("CatActividades");

            // 4) Último recurso: primera fila + warning (NO debería llegar aquí)
            logger.LogWarning(
                "CatActividades sin marca Principal y CodigoActividad={Codigo} no encontrada en la tabla. "
                + "Usando la primera actividad disponible como fallback.",
                codigoConfig);
            return (await db.CatActividades
                .AsNoTracking()
                .OrderBy(a => a.Id)
                .FirstAsync(ct))
                .CodigoCaeb;
        }
    }
}