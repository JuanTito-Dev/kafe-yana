using KafeYana.Api.Filters;
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
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class MesaController(IMesaRepositorio _Mesa, IUnitWork _db, IVentaServices _venta, Detalle_RondaService _detalleRondaService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoMesaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mesa = datos.Adapt<Mesa>();

            await _Mesa.Crear(mesa);

            await _Mesa.SaveAsync();

            return Created("", new {message = "Mesa creada"});
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Editar(DtoMesaCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mesadb = await _Mesa.FindByIdAsync(Id);

            if (mesadb == null) return NotFound(new { message = "Mesa no encontrada" });

            datos.Adapt(mesadb);

            await _Mesa.SaveAsync();

            return Ok(new {message = "Mesa actualizada"});
            
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var mesadb = await _Mesa.FindByIdAsync(Id);

            if (mesadb is null) return NotFound(new { message = "Mesa no existe" });

            if (mesadb.Id_Pedido is not null) return BadRequest("No puedes eliminar esta mesa hasta terminar el pedido"); 

            await _Mesa.Remove(mesadb);

            await _Mesa.SaveAsync();

            return Ok(new {message = "Mesa eliminada"});
        }

        [HttpPost("Ocupar/{Id:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<ActionResult<DtoMesaRespuesta>> Iniciar(int Id, DtoniciarMesa datos)
        {
            var mesa = await _db.mesas.FindByIdAsync(Id);

            if (mesa is null) return NotFound(new { message = "Mesa no existe" });

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

            if (mesa is null) return NotFound(new { message = "Mesa no existe" });

            if (mesa.pedido.Total > 0) return NotFound(new { message = "No puedes liberar un pedido sin antes cobrar" });

            await _db.Pedidos.Remove(mesa.pedido);

            mesa.Disponible = true;

            await _db.SaveUnitWork();

            return Ok(new {message = "Mesa libre"});
        }

        [HttpPost("ronda/{Id:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> AgregarRonda(int Id, DtoRondaAgregar datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Verificaciones de mesa
            var mesa = await _db.mesas.GetMesaPedido(Id);

            if (mesa is null) 
                return NotFound(new { message = "Mesa no existe" });

            if (mesa.pedido == null) 
                return NotFound(new { message = "La mesa no tiene un pedido activo" });

            if (mesa.Id_Pedido != datos.Id_Pedido) 
                return BadRequest(new { message = "El pedido no corresponde a la mesa" });

            if (datos.Detalles.Count <= 0) 
                return BadRequest(new { message = "No se han agregado productos a la ronda" });

            var ronda = await _detalleRondaService.CrearRondaConDetallesAsync(datos.Id_Pedido, datos.Detalles);

            // Agregar ronda al pedido de la mesa
            await _db.rondas.Crear(ronda);

            mesa.pedido.Total += ronda.SubTotal;

            // Guardar cambios
            await _db.SaveUnitWork();

            return Ok(new
            {
                message      = "Ronda agregada correctamente",
                nombre_mesa  = mesa.Nombre,
                numero_orden = mesa.pedido.Id,
                ronda = new
                {
                    ronda.Id,
                    ronda.Ronda_Descripcion,
                    ronda.SubTotal,
                    detalles = ronda.Detalle.Select(d => new
                    {
                        nombre    = d.Nombre_Producto,
                        cantidad  = d.Cantidad,
                        precio    = d.Precio,
                        ubicacion = d.Ubicacion,
                        opciones  = d.Opciones.Select(o => new
                        {
                            nombre        = o.Opcion!.Nombre,
                            ajuste_precio = o.Opcion.AjustePrecio,
                            cambios       = o.Opcion.Ajustes.Select(a => new
                            {
                                tipo    = a.TipoAjuste,
                                sale    = a.InsumoBase.Nombre,
                                entra   = a.InsumoNuevo != null ? a.InsumoNuevo.Nombre : (string?)null,
                                cantidad = a.Cantidad,
                                unidad  = a.InsumoBase.Unidad_min_uso
                            })
                        }),
                        items_combo = d.ItemsCombo.Select(i => new
                        {
                            nombre    = i.Nombre,
                            cantidad  = i.Cantidad,
                            ubicacion = i.Ubicacion
                        })
                    })
                }
            });

        }

        [HttpPost("cobrar/{Id:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Cobrar(int Id, DtoVentaPedido datos)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            // Obtener mesa con pedido
            var mesa = await _db.mesas.GetMesaPedido(Id);
            if (mesa is null)
                return NotFound(new { message = "Mesa no existe" });

            // Validar pedido corresponde a la mesa
            if (!await _db.mesas.MesaConpedido(datos.Id_Pedido, Id_mesa: Id))
                return NotFound(new { message = "El pedido no corresponde a la mesa" });

            // Obtener usuario actual
            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            // Validar que el total de pagos coincide con el total del pedido
            var pedido = await _db.Pedidos.FindByIdAsync(datos.Id_Pedido);
            if (pedido is null)
                return NotFound(new { message = "Pedido no encontrado" });

            if (datos.Pagos.Total != pedido.Total)
                return BadRequest(new { message = "El total de los pagos no coincide con el total del pedido." });

            // Procesar venta
            var venta = await _venta.ProcesarVenta(datos.Id_Pedido, datos.Id_Cliente, nombreUsuario, datos.Pagos);

            // Guardar venta
            await _db.ventas.Crear(venta);

            // Liberar mesa
            mesa.Disponible = true;

            var caja = (Caja)HttpContext.Items["Caja"]!;
            caja.RegistrarVenta(datos.Pagos.Efectivo, datos.Pagos.Tarjeta, datos.Pagos.Qr);

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
