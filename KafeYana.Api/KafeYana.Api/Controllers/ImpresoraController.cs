using KafeYana.Application.Dtos.ImpresoraDtos;
using KafeYana.Application.IServicios;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class ImpresoraController(IImpresoraService _impresora) : ControllerBase
    {
        [HttpPost("pedido")]
        public async Task<IActionResult> Pedido(PedidoImprimirDto dto)
        {
            if (!dto.Items.Any()) return BadRequest(new { error = "Sin items" });
            var resultado = await _impresora.EnviarPedidoAsync(dto);
            if (!resultado.Any()) return Ok(new { ok = true, msg = "Ningún destino válido, nada que imprimir" });
            int status = resultado.All(r => r.Ok) ? 200 : 207;
            return StatusCode(status, resultado);
        }

        [HttpPost("cuenta")]
        public async Task<IActionResult> Cuenta(CuentaImprimirDto dto)
        {
            if (!dto.Items.Any()) return BadRequest(new { error = "Sin items" });
            var resultado = await _impresora.EnviarCuentaAsync(dto);
            int status = resultado.All(r => r.Ok) ? 200 : 207;
            return StatusCode(status, resultado);
        }

        [HttpPost("recibo")]
        public async Task<IActionResult> Recibo(ReciboImprimirDto dto)
        {
            var resultado = await _impresora.EnviarReciboAsync(dto);
            int status = resultado.All(r => r.Ok) ? 200 : 207;
            return StatusCode(status, resultado);
        }

        [HttpPost("catalogo")]
        public async Task<IActionResult> Catalogo(CatalogoImprimirDto dto)
        {
            await _impresora.EnviarCatalogoAsync(dto);
            return Ok(new { ok = true, total = dto.Productos.Count });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { ok = true, ts = DateTime.UtcNow });
    }
}
