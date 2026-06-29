using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KafeYana.Api.Controllers
{
    /// <summary>
    /// FIX #5 — endpoint público de estado de contingencia para el banner del frontend.
    /// <c>GET /api/contingencia/estado</c> devuelve un resumen minimalista del estado
    /// de contingencia actual: si hay contingencia activa, cuántas ventas están
    /// pendientes de reenvío, y el último código de estado de paquete observado.
    ///
    /// Es distinto de <c>EventoSignificativoController.GET /api/evento-significativo/estado</c>
    /// que devuelve el detalle completo (catalogado, sólo admin). Este endpoint es
    /// liviano y accesible a cualquier usuario autenticado (cajero incluido) para que
    /// el banner del POS pueda mostrar progreso real sin necesidad de un dashboard admin.
    /// </summary>
    [ApiController]
    [Route("api/contingencia")]
    [Authorize]
    public class ContingenciaEstadoController : ControllerBase
    {
        private readonly IUnitWork _unit;
        private readonly IEventoSignificativoSiatService _eventoSvc;

        public ContingenciaEstadoController(IUnitWork unit, IEventoSignificativoSiatService eventoSvc)
        {
            _unit = unit;
            _eventoSvc = eventoSvc;
        }

        [HttpGet("estado")]
        [ProducesResponseType(typeof(EstadoContingenciaBannerDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<EstadoContingenciaBannerDto>> GetEstado(
            CancellationToken ct = default)
        {
            // 1. ¿Hay contingencia activa? ListarContingenciasActivasAsync ya filtra
            //    EstadoContingencia=Activo y devuelve todas las (suc, pv) — un Any sobre
            //    su resultado es lo más barato.
            var contingenciasActivas = await _unit.eventosSignificativosSiat
                .ListarContingenciasActivasAsync(ct);
            var contingenciaActiva = contingenciasActivas.Count > 0;

            // 2. Ventas contingencia pendientes (huérfanas o no). TipoEmision=2 = contingencia.
            var ventasPendientes = await _unit.ventas.VentaQuery()
                .CountAsync(v => v.TipoEmision == 2
                              && v.EstadoSiat == FacturaEstado.Pendiente, ct);

            // 3. Último estado de paquete observado en la BD.
            //    901 = Pendiente (aún validando), 904 = Observada, 908 = Validada.
            //    EstadoSiat en BD se setea desde respuesta.CodigoEstado del SOAP.
            var ultimaVentaContingencia = await _unit.ventas.VentaQuery()
                .Where(v => v.TipoEmision == 2
                         && (v.EstadoSiat == FacturaEstado.Pendiente
                          || v.EstadoSiat == FacturaEstado.Validada
                          || v.EstadoSiat == FacturaEstado.Observada))
                .OrderByDescending(v => v.Id)
                .Select(v => new { v.EstadoSiat })
                .FirstOrDefaultAsync(ct);

            var ultimoEstadoPaquete = ultimaVentaContingencia?.EstadoSiat switch
            {
                FacturaEstado.Validada => "908",
                FacturaEstado.Observada => "904",
                FacturaEstado.Pendiente => "901",
                _ => "NONE"
            };

            return Ok(new EstadoContingenciaBannerDto
            {
                ContingenciaActiva = contingenciaActiva,
                VentasPendientes = ventasPendientes,
                UltimoEstadoPaquete = ultimoEstadoPaquete
            });
        }
    }

    /// <summary>
    /// FIX #5 — DTO liviano para el banner. NO debe confundirse con
    /// <see cref="EstadoContingenciaDto"/> (detallado, solo admin).
    ///
    /// Los <c>[JsonPropertyName]</c> son OBLIGATORIOS porque el backend usa
    /// <c>PropertyNamingPolicy=null</c> (ver memoria kafeyana-dtopagolinea-json-name-mismatch).
    /// Sin ellos el frontend recibe PascalCase y todos los campos caen en undefined,
    /// lo que el banner renderiza como "ned ventas pendientes ... estado undefined".
    /// </summary>
    public class EstadoContingenciaBannerDto
    {
        [JsonPropertyName("contingenciaActiva")]
        public bool ContingenciaActiva { get; set; }

        [JsonPropertyName("ventasPendientes")]
        public int VentasPendientes { get; set; }

        /// <summary>901 / 904 / 908 / NONE — código de estado SIN del último paquete.</summary>
        [JsonPropertyName("ultimoEstadoPaquete")]
        public string UltimoEstadoPaquete { get; set; } = "NONE";
    }
}
