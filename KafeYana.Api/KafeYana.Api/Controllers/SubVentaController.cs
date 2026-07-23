using KafeYana.Api.Filters;
using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class SubVentaController(
        ISubVentaService _subVenta,
        IUnitWork _db,
        IFacturaSiatAnulacionService _facturaSiatAnulacion) : ControllerBase
    {
        /// <summary>Sub-ventas (cobros parciales) aún sin factura, para "facturar después".</summary>
        [HttpGet("pendientes")]
        public async Task<IActionResult> Pendientes([FromQuery] int? idMesa, [FromQuery] int? idParaLlevar, CancellationToken ct)
        {
            var pendientes = await _subVenta.GetPendientesFacturarAsync(idMesa, idParaLlevar, ct);
            return Ok(pendientes);
        }

        /// <summary>
        /// Historial completo de sub-ventas de un pedido (facturadas y no) —
        /// fuente de verdad en BD, no depende de estado local de sesión del
        /// frontend (sobrevive a refresh / otra pestaña / otro dispositivo).
        /// </summary>
        [HttpGet("pedido/{idPedido:int}")]
        public async Task<IActionResult> PorPedido(int idPedido, CancellationToken ct)
        {
            var historial = await _subVenta.GetPorPedidoAsync(idPedido, ct);
            return Ok(historial);
        }

        /// <summary>Factura después una sub-venta que se cobró sin factura al momento.</summary>
        [HttpPost("{id:int}/facturar")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Facturar(int id, [FromBody] DtoFacturarSubVenta datos, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            var envioSiat = await _subVenta.FacturarSubVentaAsync(id, datos, nombreUsuario, ct);

            return Ok(new { message = "Sub-venta facturada correctamente", envioSiat });
        }

        /// <summary>
        /// Anula la factura de una sub-venta ya facturada y la devuelve al estado
        /// "pendiente de facturar" para poder reemitirla (nunca se edita un
        /// documento fiscal ya emitido, solo se anula y reemite).
        /// </summary>
        [HttpPost("{id:int}/anular")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Anular(int id, [FromBody] DtoAnularFactura dto, CancellationToken ct)
        {
            var subVenta = await _db.subventas.FindByIdAsync(id)
                ?? throw new VentaException("Sub-venta no encontrada.");

            if (!subVenta.Facturada || subVenta.Id_Venta is not int ventaId)
                throw new VentaException("Esta sub-venta no tiene una factura emitida para anular.");

            var anulacion = await _facturaSiatAnulacion.AnularVentaAsync(ventaId, dto.CodigoMotivo, ct);

            if (anulacion.Transaccion)
            {
                subVenta.Facturada = false;
                subVenta.Id_Venta = null;
                await _db.SaveUnitWork();
            }

            var mensaje = anulacion.Transaccion
                ? "Factura anulada; la sub-venta vuelve a estar pendiente de facturar."
                : "El SIAT rechazó la anulación o hubo error de comunicación.";

            return Ok(new { message = mensaje, anulacion });
        }
    }
}
