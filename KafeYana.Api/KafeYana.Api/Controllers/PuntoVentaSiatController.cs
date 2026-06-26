using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
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
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class PuntoVentaSiatController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PuntoVentaSiatController(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// GET /api/PuntoVentaSiat/activos
        ///
        /// Lista los puntos de venta activos. El frontend usa esta lista para
        /// popular el dropdown del navbar. El orden es estable
        /// (CodigoSucursal, CodigoPuntoVenta) para que siempre se muestre igual.
        ///
        /// No requiere filtro por sucursal porque es catálogo global del sistema;
        /// la autorización ya está cubierta por el [Authorize] del controller.
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
    }

    /// <summary>
    /// DTO expuesto al frontend. NO expone Id interno de BD ni campos de auditoría
    /// (UltimaSyncActividades / UltimaSyncMotivoAnulacion) — el frontend solo
    /// necesita los códigos SIAT y el nombre para mostrar.
    /// </summary>
    public class PuntoVentaSiatActivoDto
    {
        public int CodigoSucursal { get; set; }
        public int CodigoPuntoVenta { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
