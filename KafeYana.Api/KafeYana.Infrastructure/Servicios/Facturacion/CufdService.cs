using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class CufdService : ICufdService
    {
        // Constantes por modo de emisión SIAT.
        //  - codigoEmision = 1 (Computarizado en Línea): el SIAT compara fechaEmision
        //    contra su hora actual y rechaza con 1009 cuando la diferencia supera
        //    unos minutos (observado: rechazo claro a partir de ~30 min, conservamos
        //    5 min de margen). Cualquier CUFD reusado que ya tenga más de esto
        //    causará el error.
        //  - codigoEmision = 2/3/4 (Fuera de línea / Masivo / Contingencia):
        //    el SIAT no compara contra hora actual, se puede reusar el CUFD durante
        //    toda su vigencia oficial (~24 h) sin pedir uno nuevo en cada cobro.
        // El filtro "FechaVigencia > DateTime.UtcNow" en ObtenerCufdVigenteAsync
        // actúa como segunda barrera: si el SIAT marcó el CUFD como vencido, se
        // solicita uno nuevo aunque la antigüedad interna sea inferior a este límite.
        private static readonly TimeSpan AntiguedadMaximaCufdEnLinea = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan AntiguedadMaximaCufdOffline = TimeSpan.FromDays(1);

        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IOptions<SiatOptions> _siatOptions;
        private readonly ILogger<CufdService> _logger;

        /// <summary>
        /// Antigüedad máxima permitida del CUFD para reusarlo entre cobros,
        /// calculada según el contexto. Contingencia siempre 24 h (el SIAT no compara
        /// contra hora actual en este modo). Resto: según <c>CodigoEmision</c> de
        /// appsettings: en línea = 5 min (evita SIAT 1009); offline/masivo = 24 h.
        /// </summary>
        private TimeSpan AntiguedadMaximaCufd(bool esContingencia)
        {
            if (esContingencia) return AntiguedadMaximaCufdOffline;
            return _siatOptions.Value.CodigoEmision == 1
                ? AntiguedadMaximaCufdEnLinea
                : AntiguedadMaximaCufdOffline;
        }

        public CufdService(
            SiatHttpClient siat,
            ICuisService cuisService,
            IDbContextFactory<AppDbContext> dbFactory,
            IOptions<SiatOptions> siatOptions,
            ILogger<CufdService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _dbFactory = dbFactory;
            _siatOptions = siatOptions;
            _logger = logger;
        }

        public async Task<Cufd> SolicitarCufdAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default,
            bool bypassCortocircuito = false)
        {
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
            var resp = await _siat.SolicitarCufdAsync(
                cuis.Codigo, codigoSucursal, codigoPuntoVenta, ct, bypassCortocircuito);

            if (string.IsNullOrWhiteSpace(resp.CodigoCufd))
            {
                var errores = FormatearErroresSiat(resp.CodigosRespuesta);
                _logger.LogWarning(
                    "SIAT sin código CUFD. transaccion={Transaccion}. Mensajes: {Errores}",
                    resp.Transaccion,
                    errores);
                throw new InvalidOperationException($"SIAT rechazó CUFD: {errores}");
            }

            if (!resp.Transaccion)
            {
                _logger.LogInformation(
                    "SIAT devolvió CUFD existente (transaccion=false). Codigo: {Codigo}",
                    resp.CodigoCufd);
            }

            var cufd = new Cufd
            {
                Codigo = resp.CodigoCufd,
                CodigoControl = resp.CodigoControl ?? string.Empty,
                Direccion = resp.Direccion ?? string.Empty,
                FechaVigencia = NormalizarUtc(resp.FechaVigencia ?? DateTime.UtcNow.AddHours(24)),
                CodigoSucursal = codigoSucursal,
                CodigoPuntoVenta = codigoPuntoVenta,
                FechaRegistro = DateTime.UtcNow,
                // Guardamos la fechaEmision del SIAT con la que se pidió este CUFD.
                // Al generar el CUF después, deberemos usar EXACTAMENTE esta misma fecha,
                // si no el SIAT rechaza con 1002/1003.
                FechaEmisionSolicitud = NormalizarUtc(fechaEmision)
            };

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.Cufd.Add(cufd);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CUFD obtenido del SIAT y guardado (Id:{Id}). Vigente hasta: {Vigencia}. FechaEmisionSolicitud: {FechaSoli}",
                cufd.Id,
                cufd.FechaVigencia.ToString("yyyy-MM-dd HH:mm:ss"),
                cufd.FechaEmisionSolicitud.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            return cufd;
        }

        public async Task<Cufd> ObtenerCufdVigenteAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            DateTime fechaEmision,
            CancellationToken ct = default,
            bool bypassCortocircuito = false,
            bool esContingencia = false)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Buscar un CUFD que esté (a) vigente (FechaVigencia > NOW) y
            // (b) suficientemente reciente (FechaEmisionSolicitud dentro del umbral,
            // que depende del contexto). Contingencia: 24 h. En línea (CodigoEmision=1
            // en appsettings): 5 min (evita error SIAT 1009). Offline puro: 24 h.
            var limite = AntiguedadMaximaCufd(esContingencia);
            var vigente = await db.Cufd
                .Where(c =>
                    c.CodigoSucursal == codigoSucursal
                    && c.CodigoPuntoVenta == codigoPuntoVenta
                    && c.FechaVigencia > DateTime.UtcNow)
                .OrderByDescending(c => c.FechaRegistro)
                .FirstOrDefaultAsync(ct);

            if (vigente is not null)
            {
                var antiguedad = DateTime.UtcNow - vigente.FechaEmisionSolicitud;
                if (antiguedad <= limite)
                {
                    _logger.LogDebug(
                        "CUFD vigente reusado (Id:{Id}, codigoEmision={CodEmi}). "
                        + "Antigüedad={Ant} s, Límite={Max} min, Codigo={Codigo}",
                        vigente.Id,
                        _siatOptions.Value.CodigoEmision,
                        (long)antiguedad.TotalSeconds,
                        (long)limite.TotalMinutes,
                        vigente.Codigo);
                    return vigente;
                }

                _logger.LogInformation(
                    "CUFD vigente descartado (Id:{Id}, codigoEmision={CodEmi}) por antigüedad "
                    + "({Ant} s > {Max} s). Se solicitará uno nuevo al SIAT.",
                    vigente.Id,
                    _siatOptions.Value.CodigoEmision,
                    (long)antiguedad.TotalSeconds,
                    (long)limite.TotalSeconds);
                // Lo marcamos como vencido para que no se reconsidere en esta consulta.
                // No lo eliminamos para conservar trazabilidad histórica.
                vigente.FechaVigencia = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                _logger.LogInformation(
                    "No hay CUFD vigente para PV ({Suc},{PV}). Solicitando al SIAT...",
                    codigoSucursal, codigoPuntoVenta);
            }

            return await SolicitarCufdAsync(
                codigoSucursal, codigoPuntoVenta, fechaEmision, ct, bypassCortocircuito);
        }

        public async Task<Cufd?> ObtenerCufdEnCacheAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // 1) Preferentemente uno vigente según SIAT (FechaVigencia > NOW).
            //    NO aplicamos AntiguedadMaximaCufd porque en contingencia el CUFD
            //    puede ser viejo y aun así ser lo único que tenemos para emitir.
            var vigente = await db.Cufd
                .Where(c =>
                    c.CodigoSucursal == codigoSucursal
                    && c.CodigoPuntoVenta == codigoPuntoVenta
                    && c.FechaVigencia > DateTime.UtcNow)
                .OrderByDescending(c => c.FechaRegistro)
                .FirstOrDefaultAsync(ct);

            if (vigente is not null)
            {
                _logger.LogInformation(
                    "CufdEnCache: reusando CUFD vigente Id={Id}, código={Codigo} "
                    + "(antigüedad {Ant}s — IGNORADA, modo contingencia)",
                    vigente.Id, vigente.Codigo,
                    (long)(DateTime.UtcNow - vigente.FechaEmisionSolicitud).TotalSeconds);
                return vigente;
            }

            // 2) Fallback: el más reciente sin importar vigencia. Caso típico:
            //    server arrancó con SIAT caído → tabla vacía al boot, o el
            //    CUFD guardado ya fue marcado vencido por ObtenerCufdVigenteAsync.
            var ultimo = await db.Cufd
                .Where(c =>
                    c.CodigoSucursal == codigoSucursal
                    && c.CodigoPuntoVenta == codigoPuntoVenta)
                .OrderByDescending(c => c.FechaRegistro)
                .FirstOrDefaultAsync(ct);

            if (ultimo is not null)
            {
                _logger.LogWarning(
                    "CufdEnCache: NO hay CUFD vigente para PV ({Suc},{PV}). "
                    + "Usando el último registrado (Id={Id}, código={Codigo}) "
                    + "vencido el {Venc}. Modo contingencia DEGRADADO.",
                    codigoSucursal, codigoPuntoVenta,
                    ultimo.Id, ultimo.Codigo, ultimo.FechaVigencia);
            }
            else
            {
                _logger.LogWarning(
                    "CufdEnCache: tabla Cufd vacía para PV ({Suc},{PV}). "
                    + "Modo contingencia NO PUEDE operar.",
                    codigoSucursal, codigoPuntoVenta);
            }

            return ultimo;
        }

        private static DateTime NormalizarUtc(DateTime fecha) =>
            fecha.Kind switch
            {
                DateTimeKind.Utc => fecha,
                DateTimeKind.Local => fecha.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
            };

        private static string FormatearErroresSiat(IEnumerable<CodigoRespuesta> mensajes)
        {
            var errores = string.Join(" | ", mensajes
                .Where(m => m.Codigo != 0 || !string.IsNullOrWhiteSpace(m.Descripcion))
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            return string.IsNullOrWhiteSpace(errores)
                ? "sin mensajes del SIAT"
                : errores;
        }
    }
}