using KafeYana.Api.Filters;
using KafeYana.Api.Helpers;
using KafeYana.Api.Hubs;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class PedidoController(
        ICobroPedidoService _cobroPedido,
        IKafeYanaNotificador _notificador) : ControllerBase
    {
        [HttpPost("cobrar")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Cobrar(DtoVentaPedido datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            var caja = (Caja)HttpContext.Items["Caja"]!;
            var cobro = await _cobroPedido.CobrarPedidoActivoAsync(datos, nombreUsuario, caja);

            await _notificador.NotificarVentaProcesada(
                new VentaPayload(cobro.OrigenVenta, datos.Id_Pedido, cobro.Resultado.Venta.MontoTotal));

            if (cobro.IdMesa is int idMesa)
            {
                await _notificador.NotificarMesaActualizada(
                    new MesaActualizadaPayload(idMesa, cobro.OrigenVenta, Disponible: true, IdPedido: null));
            }
            else if (cobro.OrigenVenta == "Para llevar")
            {
                await _notificador.NotificarPedidoParaLlevarActualizado(
                    new ParaLlevarPayload(IdPedido: null, Disponible: true));
            }

            return Ok(VentaRespuestaHelper.ConstruirRespuestaCobro(cobro.Resultado, cobro.EnvioSiat));
        }
    }
}
