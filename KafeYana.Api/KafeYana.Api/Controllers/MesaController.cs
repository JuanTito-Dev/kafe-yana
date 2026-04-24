using KafeYana.Application.Dtos.MesaDtos;
using KafeYana.Application.Dtos.PedidoDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RolesKafe.Admin)]
    public class MesaController(IMesaRepositorio _Mesa, IUnitWork _db, IVentaServices _venta, Detalle_RondaService _detalleRondaService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoMesaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _Mesa.ExisteAsync(x => x.Nombre == datos.Nombre))
                throw new CampoYaExistenteFailException(datos.Nombre);

            var mesa = datos.Adapt<Mesa>();

            await _Mesa.Crear(mesa);

            await _Mesa.SaveAsync();

            return Created();
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Editar(DtoMesaCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mesadb = await _Mesa.FindByIdAsync(Id);

            if (mesadb == null) return BadRequest(new { message = "Mesa no encontrada" });

            if (datos.Nombre != mesadb.Nombre &&  await _Mesa.ExisteAsync(x => x.Nombre == datos.Nombre))
                throw new CampoYaExistenteFailException(datos.Nombre);

            datos.Adapt(mesadb);

            await _Mesa.SaveAsync();

            return NoContent();
            
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var mesadb = await _Mesa.FindByIdAsync(Id);

            if (mesadb is null) return BadRequest(new { message = "Mesa no existe" });

            await _Mesa.Remove(mesadb);

            await _Mesa.SaveAsync();

            return NoContent();
        }

        [HttpPost("Ocupar/{Id:int}")]
        public async Task<ActionResult<DtoMesaRespuesta>> Iniciar(int Id, DtoniciarMesa datos)
        {
            var mesa = await _db.mesas.FindByIdAsync(Id);

            if (mesa is null) return BadRequest(new { message = "Mesa no existe" });

            if (!mesa.Disponible) return BadRequest(new { message = "Mesa no disponible" });

            var newpedido = datos.Adapt<Pedido>();

            await _db.Pedidos.Crear(newpedido);

            mesa.pedido = newpedido;

            mesa.Disponible = false;

            await _db.SaveUnitWork();

            var respuesta = new DtoMesaRespuesta
            {
                Id = mesa.Id,
                Nombre = mesa.Nombre,
                Id_Pedido = mesa.pedido?.Id,
                Disponible = mesa.Disponible,
                pedido = newpedido.Adapt<DtoPedidoRespuesta>()
            };

            return Ok(respuesta);
        }

        [HttpPut("Liberar/{Id:int}")]
        public async Task<IActionResult> Liberar(int Id)
        {
            var mesa = await _db.mesas.GetMesaPedido(Id);

            if (mesa is null) return BadRequest(new { message = "Mesa no existe" });

            if (mesa.pedido.Total > 0) return BadRequest(new { message = "No puedes liberar un pedido sin antes cobrar" });

            await _db.Pedidos.Remove(mesa.pedido);

            mesa.Disponible = true;

            await _db.SaveUnitWork();

            return NoContent();
        }

        [HttpPost("ronda/{Id:int}")]
        public async Task<IActionResult> AgregarRonda(int Id, DtoRondaAgregar datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Verificaciones de mesa
            var mesa = await _db.mesas.GetMesaPedido(Id);

            if (mesa is null) 
                return BadRequest(new { message = "Mesa no existe" });

            if (mesa.pedido == null) 
                return BadRequest(new { message = "La mesa no tiene un pedido activo" });

            if (mesa.Id_Pedido != datos.Id_Pedido) 
                return BadRequest(new { message = "El pedido no corresponde a la mesa" });

            if (datos.Detalles.Count <= 0) 
                return BadRequest(new { message = "No se han agregado productos a la ronda" });

            var ronda = await _detalleRondaService.CrearRondaConDetallesAsync(datos.Id_Pedido, datos.Detalles);

            // Agregar ronda al pedido de la mesa
            mesa.pedido.Rondas.Add(ronda);
            mesa.pedido.Total += ronda.SubTotal;

            // Guardar cambios
            await _db.SaveUnitWork();

            return Ok(new { message = "Ronda agregada correctamente", ronda = new { ronda.Id, ronda.Ronda_Descripcion, ronda.SubTotal, detalles = ronda.Detalle.Count } });

        }

        [HttpPost("cobrar/{Id:int}")]
        public async Task<IActionResult> Cobrar(int Id, DtoVentaPedido datos)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            // Obtener mesa con pedido
            var mesa = await _db.mesas.GetMesaPedido(Id);
            if (mesa is null)
                return BadRequest(new { message = "Mesa no existe" });

            // Validar pedido corresponde a la mesa
            if (!await _db.mesas.MesaConpedido(datos.Id_Pedido, Id_mesa: Id))
                return BadRequest(new { message = "El pedido no corresponde a la mesa" });

            // Obtener usuario actual
            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            // Procesar venta
            var venta = await _venta.ProcesarVenta(datos.Id_Pedido, datos.Id_Cliente, nombreUsuario, datos.TipoPago.ToString());

            // Guardar venta
            await _db.ventas.Crear(venta);

            // Liberar mesa
            mesa.Disponible = true;

            // Guardar todos los cambios (transaccional)
            await _db.SaveUnitWork();

            // Respuesta con información útil
            return Ok(new
            {
                message = "Venta procesada correctamente",
            });
        }
    }
}
