using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Controllers
{
    /// <summary>
    /// Endpoints para que el frontend (POS / navbar) consulte los puntos de venta
    /// declarados ante el SIAT y active el que el cajero está usando físicamente.
    ///
    /// El cajero selecciona uno en el navbar y el backend lo usa para construir
    /// el CUF/CUFD consistente con el sobre SOAP que el SIAT valida. Esto evita
    /// el bug donde la Venta quedaba con CodigoPuntoVenta de appsettings pero CUF
    /// construido con el PV real de la BD → rechazo 1002/1003 del SIAT.
    ///
    /// Semántica single-active: el sistema trabaja con EXACTAMENTE UN PV activo
    /// a la vez. El endpoint `activar` desactiva los demás y activa el seleccionado
    /// en una transacción atómica. `VentaServices.ResolverPuntoVentaActivo` ya
    /// lanzaba `VentaException` si encontraba más de un PV activo, así que esta
    /// UI simplemente expone la operación que antes requería UPDATE manual en BD.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class PuntoVentaSiatController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICuisService _cuisService;
        private readonly ICufdService _cufdService;
        private readonly ILogger<PuntoVentaSiatController> _logger;

        public PuntoVentaSiatController(
            IDbContextFactory<AppDbContext> dbFactory,
            ICuisService cuisService,
            ICufdService cufdService,
            ILogger<PuntoVentaSiatController> logger)
        {
            _dbFactory = dbFactory;
            _cuisService = cuisService;
            _cufdService = cufdService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/PuntoVentaSiat/activos
        ///
        /// Lista los puntos de venta activos. El frontend usa esta lista para
        /// popular el dropdown del navbar cuando solo quiere ver el actual.
        /// El orden es estable (CodigoSucursal, CodigoPuntoVenta) para que
        /// siempre se muestre igual.
        ///
        /// Por la semántica single-active, esta lista siempre tiene 0 o 1 fila.
        /// </summary>
        [HttpGet("activos")]
        public async Task<IActionResult> ListarActivos(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var activos = await db.PuntosVentaSiat
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.CodigoSucursal)
                .ThenBy(p => p.CodigoPuntoVenta)
                .Select(p => new PuntoVentaSiatActivoDto
                {
                    CodigoSucursal = p.CodigoSucursal,
                    CodigoPuntoVenta = p.CodigoPuntoVenta,
                    Nombre = p.Nombre
                })
                .ToListAsync(ct);

            return Ok(activos);
        }

        /// <summary>
        /// GET /api/PuntoVentaSiat/todos
        ///
        /// Lista TODOS los puntos de venta registrados (activos e inactivos).
        /// El frontend usa este endpoint para popular el dropdown del navbar con
        /// el catálogo completo, marcando visualmente cuál es el activo y
        /// permitiendo al cajero activar otro.
        ///
        /// El orden es estable (CodigoSucursal, CodigoPuntoVenta) para que
        /// siempre se muestre igual.
        /// </summary>
        [HttpGet("todos")]
        public async Task<IActionResult> ListarTodos(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var todos = await db.PuntosVentaSiat
                .AsNoTracking()
                .OrderBy(p => p.CodigoSucursal)
                .ThenBy(p => p.CodigoPuntoVenta)
                .Select(p => new PuntoVentaSiatDto
                {
                    CodigoSucursal = p.CodigoSucursal,
                    CodigoPuntoVenta = p.CodigoPuntoVenta,
                    Nombre = p.Nombre,
                    Activo = p.Activo
                })
                .ToListAsync(ct);

            return Ok(todos);
        }

        /// <summary>
        /// POST /api/PuntoVentaSiat/{codigoSucursal}/{codigoPuntoVenta}/activar
        ///
        /// Activa el PV indicado y desactiva todos los demás en una transacción
        /// atómica. Devuelve el estado del PV recién activado.
        ///
        /// Si el PV no existe → 404.
        /// Si el PV ya era el activo → no-op (devuelve 200 con su estado actual).
        ///
        /// Concurrencia: si dos cajeros activan PVs distintos al mismo tiempo, el
        /// último gana. Aceptable para el flujo actual; si se requiere consistencia
        /// más fuerte, evaluar `SELECT ... FOR UPDATE` o lock a nivel de aplicación.
        /// </summary>
        [HttpPost("{codigoSucursal:int}/{codigoPuntoVenta:int}/activar")]
        public async Task<IActionResult> Activar(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // 1) Verificar que el PV destino existe
            var destino = await db.PuntosVentaSiat
                .FirstOrDefaultAsync(p =>
                    p.CodigoSucursal == codigoSucursal &&
                    p.CodigoPuntoVenta == codigoPuntoVenta, ct);

            if (destino is null)
            {
                return NotFound(new
                {
                    transaccion = false,
                    error = $"No existe el PV (Suc={codigoSucursal}, PV={codigoPuntoVenta})."
                });
            }

            try
            {
                // 2) Desactivar todos los demás (bulk update idiomático EF Core 7+)
                await db.PuntosVentaSiat
                    .Where(p => !(p.CodigoSucursal == codigoSucursal
                               && p.CodigoPuntoVenta == codigoPuntoVenta))
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Activo, false), ct);

                // 3) Activar el seleccionado (solo si no lo estaba ya)
                if (!destino.Activo)
                {
                    destino.Activo = true;
                    await db.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "PV activado: (Suc={Suc}, PV={PV}) '{Nombre}'",
                    destino.CodigoSucursal, destino.CodigoPuntoVenta, destino.Nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error activando PV (Suc={Suc}, PV={PV})",
                    codigoSucursal, codigoPuntoVenta);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }

            // 4) Warm-up: si el PV nunca tuvo un cobro online, su tabla Cufd/Cuis
            //    queda vacía y una contingencia disparada antes del primer cobro
            //    no tiene CUFD que citar (ObtenerCufdEnCacheAsync no encuentra
            //    nada → CufdEvento="" → paquete sale con CUFD en cero). Precargamos
            //    CUIS/CUFD apenas se activa el PV, si el SIAT responde.
            //    Best-effort: si el SIAT está caído justo al activar, no bloqueamos
            //    el toggle — el PV queda activo igual, degradado hasta el próximo
            //    cobro/reintento exitoso.
            try
            {
                await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
                await _cufdService.ObtenerCufdVigenteAsync(
                    codigoSucursal, codigoPuntoVenta, SiatFechaEmision.AhoraUtc(), ct);
                _logger.LogInformation(
                    "Warm-up CUIS/CUFD OK para PV recién activado (Suc={Suc}, PV={PV})",
                    codigoSucursal, codigoPuntoVenta);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Warm-up CUIS/CUFD falló para PV recién activado (Suc={Suc}, PV={PV}). "
                  + "El PV queda activo; una contingencia antes del primer cobro exitoso "
                  + "puede quedar con CUFD vacío hasta que el SIAT responda.",
                    codigoSucursal, codigoPuntoVenta);
            }

            // 5) Devolver el estado del PV recién activado
            return Ok(new PuntoVentaSiatDto
            {
                CodigoSucursal = destino.CodigoSucursal,
                CodigoPuntoVenta = destino.CodigoPuntoVenta,
                Nombre = destino.Nombre,
                Activo = destino.Activo
            });
        }

        /// <summary>
        /// PATCH /api/PuntoVentaSiat/{codigoSucursal}/{codigoPuntoVenta}/cafc
        ///
        /// Configura el CAFC (Código de Autorización de Facturación por Contingencia)
        /// que el SIN emitió específicamente para este PV, para usar en motivos 5/6/7
        /// (talonario/manual). El SIN lo emite por punto de venta vía Oficina Virtual —
        /// no hay SOAP para pedirlo automáticamente. Usar el CAFC de otro PV es inválido
        /// y el SIAT rechaza con [1045] "VALOR DE CAFC NO VALIDO".
        ///
        /// Enviar cafc=null o vacío limpia el valor (el PV queda sin CAFC — el sistema
        /// no manda &lt;cafc&gt; en el sobre SOAP para ese PV hasta que se cargue uno).
        /// </summary>
        [HttpPatch("{codigoSucursal:int}/{codigoPuntoVenta:int}/cafc")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> ActualizarCafc(
            int codigoSucursal,
            int codigoPuntoVenta,
            [FromBody] ActualizarCafcDto dto,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var destino = await db.PuntosVentaSiat
                .FirstOrDefaultAsync(p =>
                    p.CodigoSucursal == codigoSucursal &&
                    p.CodigoPuntoVenta == codigoPuntoVenta, ct);

            if (destino is null)
            {
                return NotFound(new
                {
                    transaccion = false,
                    error = $"No existe el PV (Suc={codigoSucursal}, PV={codigoPuntoVenta})."
                });
            }

            destino.Cafc = string.IsNullOrWhiteSpace(dto.Cafc) ? null : dto.Cafc.Trim();
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CAFC actualizado para PV (Suc={Suc}, PV={PV}): {Estado}",
                codigoSucursal, codigoPuntoVenta,
                destino.Cafc is null ? "(limpiado)" : "cargado");

            return Ok(new PuntoVentaSiatDto
            {
                CodigoSucursal = destino.CodigoSucursal,
                CodigoPuntoVenta = destino.CodigoPuntoVenta,
                Nombre = destino.Nombre,
                Activo = destino.Activo,
                Cafc = destino.Cafc
            });
        }
    }

    public class ActualizarCafcDto
    {
        public string? Cafc { get; set; }
    }

    /// <summary>
    /// DTO legacy — solo expone el PV activo. Se mantiene por compat con el
    /// dropdown antiguo y porque los sincronizadores SIAT lo consumen (aunque
    /// con single-active siempre devuelve 0 o 1 fila).
    /// NO expone `Activo` (siempre true) ni `Id` interno de BD ni columnas de
    /// auditoría (UltimaSync*) — el frontend solo necesita los códigos SIAT
    /// y el nombre para mostrar.
    /// </summary>
    public class PuntoVentaSiatActivoDto
    {
        public int CodigoSucursal { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO completo — incluye `Activo` para que el frontend pueda mostrar
    /// visualmente cuáles están activos y cuáles no. NO expone `Id` interno
    /// de BD ni columnas de auditoría (UltimaSync*).
    /// </summary>
    public class PuntoVentaSiatDto
    {
        public int CodigoSucursal { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? Cafc { get; set; }
    }
}
