using KafeYana.Api.Filters;
using KafeYana.Api.Helpers;
using KafeYana.Api.Hubs;
using KafeYana.Application.Dtos.MesaDtos;
using KafeYana.Application.Dtos.PedidoDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Dtos.RondaDtos;
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
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class MesaController(IMesaRepositorio _Mesa, IUnitWork _db, ICobroPedidoService _cobroPedido, Detalle_RondaService _detalleRondaService, IRondaPedidoService _rondaPedidoService, IKafeYanaNotificador _notificador, StockPayloadService _stockService) : ControllerBase
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

            await _notificador.NotificarMesaActualizada(new MesaActualizadaPayload(
                mesa.Id, mesa.Nombre, mesa.Disponible, mesa.pedido?.Id));

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

            if (mesa.pedido.Total > 0) return Conflict(new { message = "No puedes liberar un pedido sin antes cobrar" });

            var tieneSubVentas = await _db.subventas.Query().AnyAsync(s => s.Id_Pedido == mesa.pedido.Id);

            if (tieneSubVentas)
            {
                // Igual que el cierre automático en SubVentaService.CrearSubVentaAsync:
                // el Pedido queda huérfano en BD, las sub-ventas ya registradas (cobradas
                // y/o facturadas) lo siguen referenciando (FK Restrict) para historial.
                mesa.Id_Pedido = null;
                mesa.pedido = null;
            }
            else
            {
                await _db.Pedidos.Remove(mesa.pedido);
            }

            mesa.Disponible = true;

            await _db.SaveUnitWork();

            await _notificador.NotificarMesaActualizada(new MesaActualizadaPayload(
                mesa.Id, mesa.Nombre, mesa.Disponible, IdPedido: null));

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
            await _db.SaveUnitWork();

            await _rondaPedidoService.RecalcularTotalPedidoAsync(datos.Id_Pedido);

            // Guardar cambios
            await _db.SaveUnitWork();

            var rondaPayload = BuildRondaPayload(mesa.Nombre, mesa.pedido.Id, ronda);
            var stockPayload = await _stockService.BuildAsync(ronda);

            await _notificador.NotificarNuevaRonda(rondaPayload);
            await _notificador.NotificarStockActualizado(stockPayload);

            return Ok(new
            {
                message      = "Ronda agregada correctamente",
                nombre_mesa  = rondaPayload.NombreMesa,
                numero_orden = rondaPayload.NumeroOrden,
                ronda = new
                {
                    Id           = rondaPayload.RondaId,
                    Descripcion  = rondaPayload.RondaDescripcion,
                    rondaPayload.SubTotal,
                    detalles     = rondaPayload.Detalles
                }
            });

        }

        [HttpPut("{idMesa:int}/ronda/{idRonda:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EditarRonda(int idMesa, int idRonda, DtoRondaEditar datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mesa = await _db.mesas.GetMesaPedido(idMesa);
            if (mesa is null) return NotFound(new { message = "Mesa no existe" });
            if (mesa.pedido is null) return NotFound(new { message = "La mesa no tiene un pedido activo" });
            if (mesa.Id_Pedido != datos.Id_Pedido)
                return BadRequest(new { message = "El pedido no corresponde a la mesa" });

            var ronda = await _rondaPedidoService.EditarRondaAsync(idRonda, datos);
            await _db.SaveUnitWork();

            var stockPayload = await _stockService.BuildAsync(ronda);
            await _notificador.NotificarStockActualizado(stockPayload);

            var pedido = await _db.Pedidos.FindByIdAsync(datos.Id_Pedido);
            return Ok(new { message = "Ronda actualizada", rondaId = ronda.Id, ronda.SubTotal, totalPedido = pedido?.Total });
        }

        [HttpDelete("{idMesa:int}/ronda/{idRonda:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EliminarRonda(int idMesa, int idRonda, [FromQuery] int idPedido)
        {
            var mesa = await _db.mesas.GetMesaPedido(idMesa);
            if (mesa is null) return NotFound(new { message = "Mesa no existe" });
            if (mesa.pedido is null) return NotFound(new { message = "La mesa no tiene un pedido activo" });
            if (mesa.Id_Pedido != idPedido)
                return BadRequest(new { message = "El pedido no corresponde a la mesa" });

            await _rondaPedidoService.EliminarRondaAsync(idRonda, idPedido);
            await _db.SaveUnitWork();

            await _notificador.NotificarStockActualizado(await _stockService.BuildPorPedidoAsync(idPedido));

            var pedido = await _db.Pedidos.FindByIdAsync(idPedido);
            return Ok(new { message = "Ronda eliminada", totalPedido = pedido?.Total });
        }

        [HttpPut("detalle/{idDetalle:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EditarDetalle(int idDetalle, [FromQuery] int idPedido, DtoRondadetalle datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var detalle = await _rondaPedidoService.EditarDetalleAsync(idDetalle, datos, idPedido);
            await _db.SaveUnitWork();

            var ronda = await _db.rondas.TraerConDetallesAsync(detalle.Id_Ronda);
            if (ronda is not null)
                await _notificador.NotificarStockActualizado(await _stockService.BuildAsync(ronda));

            return Ok(new { message = "Detalle actualizado", detalle.Id, detalle.Cantidad, detalle.Precio });
        }

        [HttpDelete("detalle/{idDetalle:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EliminarDetalle(int idDetalle, [FromQuery] int idPedido)
        {
            await _rondaPedidoService.EliminarDetalleAsync(idDetalle, idPedido);
            await _db.SaveUnitWork();

            await _notificador.NotificarStockActualizado(await _stockService.BuildPorPedidoAsync(idPedido));

            return Ok(new { message = "Detalle eliminado" });
        }

        [HttpPost("cobrar/{Id:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Cobrar(int Id, DtoVentaPedido datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            var caja = (Caja)HttpContext.Items["Caja"]!;
            var cobro = await _cobroPedido.CobrarMesaAsync(Id, datos, nombreUsuario, caja);

            if (cobro.EsAbono)
            {
                if (cobro.MesaCerrada && cobro.IdMesa is int mesaId)
                {
                    await _notificador.NotificarMesaActualizada(
                        new MesaActualizadaPayload(mesaId, cobro.OrigenVenta, Disponible: true, IdPedido: null));
                }

                // Si esta sub-venta facturó, incluir los mismos campos que el
                // cobro completo (VentaId, NumeroFactura, CUF, etc.) para que
                // la pantalla de éxito pueda ofrecer "Imprimir factura SIAT".
                if (cobro.Resultado is not null)
                {
                    await _notificador.NotificarVentaProcesada(
                        new VentaPayload(cobro.OrigenVenta, datos.Id_Pedido, cobro.Resultado.Venta.MontoTotal));
                }

                var venta = cobro.Resultado?.Venta;
                return Ok(new
                {
                    EsAbono = true,
                    TotalCobrado = cobro.MontoCubierto ?? 0m,
                    mesaCerrada = cobro.MesaCerrada,
                    pedidoActualizado = cobro.PedidoActualizado,
                    CodigoVenta = venta?.Cuf,
                    VentaId = venta?.Id,
                    NumeroFactura = venta?.NumeroFactura,
                    Facturado = venta?.Facturado,
                    EstadoSiat = cobro.EnvioSiat?.EstadoSiat ?? venta?.EstadoSiat,
                    CodigoRecepcion = cobro.EnvioSiat?.CodigoRecepcion ?? venta?.CodigoRecepcion,
                    SiatAceptada = cobro.EnvioSiat?.Transaccion
                        ?? (venta is not null && venta.EstadoSiat == FacturaEstado.Validada),
                    ErrorSiat = cobro.EnvioSiat?.ErrorMensaje ?? venta?.ErrorMensaje,
                    CodigoHash = venta?.CodigoHash,
                });
            }

            if (cobro.IdMesa is int idMesa)
            {
                await _notificador.NotificarMesaActualizada(
                    new MesaActualizadaPayload(idMesa, cobro.OrigenVenta, Disponible: true, IdPedido: null));
            }

            await _notificador.NotificarVentaProcesada(
                new VentaPayload(cobro.OrigenVenta, datos.Id_Pedido, cobro.Resultado!.Venta.MontoTotal));

            return Ok(VentaRespuestaHelper.ConstruirRespuestaCobro(cobro.Resultado!, cobro.EnvioSiat));
        }

        [HttpDelete("abono/{abonoId:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> RevertirAbono(int abonoId)
        {
            var pedidoActualizado = await _cobroPedido.RevertirAbonoAsync(abonoId);
            return Ok(new { message = "Pago parcial revertido", pedidoActualizado });
        }

        // ─── helper compartido ────────────────────────────────────────────────
        private static NuevaRondaPayload BuildRondaPayload(string nombreMesa, int numeroPedido, KafeYana.Domain.Entities.Inventario.Ronda ronda)
        {
            var detalles = ronda.Detalle.Select(d => new RondaDetalleItem(
                Nombre    : d.Nombre_Producto,
                Cantidad  : d.Cantidad,
                Precio    : d.Precio,
                Ubicacion : d.Ubicacion,
                Opciones  : d.Opciones.Select(o => new OpcionItem(
                    Nombre       : o.Opcion!.Nombre,
                    AjustePrecio : o.Opcion.AjustePrecio,
                    Cambios      : o.Opcion.Ajustes.Select(a => new CambioItem(
                        Tipo     : a.TipoAjuste,
                        Sale     : a.InsumoBase.Nombre,
                        Entra    : a.InsumoNuevo?.Nombre,
                        Cantidad : a.Cantidad,
                        Unidad   : a.InsumoBase.Unidad_min_uso
                    ))
                )),
                ItemsCombo: d.ItemsCombo.Select(i => new ComboItem(i.Nombre, i.Cantidad, i.Ubicacion))
            ));

            return new NuevaRondaPayload(nombreMesa, numeroPedido, ronda.Id, ronda.Ronda_Descripcion, ronda.SubTotal, detalles);
        }
    }
}
