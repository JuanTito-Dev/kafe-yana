using KafeYana.Api.Filters;
using KafeYana.Api.Helpers;
using KafeYana.Api.Hubs;
using KafeYana.Application.Dtos.MesaDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;
using KafeYana.Infrastructure.Servicios.Facturacion;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class VentaController(IUnitWork _db, Detalle_RondaService _detalleRondaService, IRondaPedidoService _rondaPedidoService, ICobroPedidoService _cobroPedido, IFacturaSiatEnvioService _facturaSiatEnvio, IKafeYanaNotificador _notificador, StockPayloadService _stockService) : ControllerBase
    {
        [HttpPost("pedido")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> CrearPedido(DtoIniciarParaLlevar datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();

            if (paraLlevar is null)
                return NotFound(new { message = "Configuración para llevar no encontrada" });

            if (!paraLlevar.Disponible)
                return BadRequest(new { message = "Ya existe un pedido para llevar activo" });

            var cliente = await ClientePedidoHelper.VincularClienteAlPedidoAsync(
                _db, datos.Id_Cliente, datos.Nombre, datos.Dni);

            var nuevoPedido = new Pedido
            {
                Id_Cliente = cliente?.Id
            };

            await _db.Pedidos.Crear(nuevoPedido);

            paraLlevar.Pedido = nuevoPedido;
            paraLlevar.Disponible = false;

            await _db.SaveUnitWork();

            await _notificador.NotificarPedidoParaLlevarActualizado(
                new ParaLlevarPayload(nuevoPedido.Id, Disponible: false));

            return Ok(new
            {
                message = "Pedido para llevar creado",
                Id_Pedido = nuevoPedido.Id
            });
        }

        [HttpPost("ronda")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> AgregarRonda(DtoRondaAgregar datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();

            if (paraLlevar is null)
                return NotFound(new { message = "Configuración para llevar no encontrada" });

            if (paraLlevar.Pedido is null)
                return NotFound(new { message = "No hay un pedido para llevar activo" });

            if (paraLlevar.Id_Pedido != datos.Id_Pedido)
                return BadRequest(new { message = "El pedido no corresponde al pedido para llevar activo" });

            if (datos.Detalles.Count <= 0)
                return BadRequest(new { message = "No se han agregado productos a la ronda" });

            var ronda = await _detalleRondaService.CrearRondaConDetallesAsync(datos.Id_Pedido, datos.Detalles);

            await _db.rondas.Crear(ronda);
            await _db.SaveUnitWork();

            await _rondaPedidoService.RecalcularTotalPedidoAsync(datos.Id_Pedido);

            await _db.SaveUnitWork();

            var rondaPayload = BuildRondaPayload("Para llevar", paraLlevar.Pedido.Id, ronda);
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
                    Id          = rondaPayload.RondaId,
                    Descripcion = rondaPayload.RondaDescripcion,
                    rondaPayload.SubTotal,
                    detalles    = rondaPayload.Detalles
                }
            });
        }

        [HttpPut("ronda/{idRonda:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EditarRonda(int idRonda, DtoRondaEditar datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();
            if (paraLlevar?.Pedido is null)
                return NotFound(new { message = "No hay un pedido para llevar activo" });
            if (paraLlevar.Id_Pedido != datos.Id_Pedido)
                return BadRequest(new { message = "El pedido no corresponde al pedido para llevar activo" });

            var ronda = await _rondaPedidoService.EditarRondaAsync(idRonda, datos);
            await _db.SaveUnitWork();

            await _notificador.NotificarStockActualizado(await _stockService.BuildAsync(ronda));

            var pedido = await _db.Pedidos.FindByIdAsync(datos.Id_Pedido);
            return Ok(new { message = "Ronda actualizada", rondaId = ronda.Id, ronda.SubTotal, totalPedido = pedido?.Total });
        }

        [HttpDelete("ronda/{idRonda:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> EliminarRonda(int idRonda, [FromQuery] int idPedido)
        {
            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();
            if (paraLlevar?.Pedido is null)
                return NotFound(new { message = "No hay un pedido para llevar activo" });
            if (paraLlevar.Id_Pedido != idPedido)
                return BadRequest(new { message = "El pedido no corresponde al pedido para llevar activo" });

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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
            var cobro = await _cobroPedido.CobrarParaLlevarAsync(datos, nombreUsuario, caja);

            if (cobro.EsAbono)
            {
                if (cobro.MesaCerrada)
                {
                    await _notificador.NotificarPedidoParaLlevarActualizado(
                        new ParaLlevarPayload(IdPedido: null, Disponible: true));
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

            await _notificador.NotificarVentaProcesada(
                new VentaPayload(cobro.OrigenVenta, datos.Id_Pedido, cobro.Resultado!.Venta.MontoTotal));
            await _notificador.NotificarPedidoParaLlevarActualizado(
                new ParaLlevarPayload(IdPedido: null, Disponible: true));

            return Ok(VentaRespuestaHelper.ConstruirRespuestaCobro(cobro.Resultado!, cobro.EnvioSiat));
        }

        [HttpPost("{id:int}/enviar-siat")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> EnviarSiat(int id)
        {
            var envioSiat = await _facturaSiatEnvio.ReenviarFacturaAsync(id);

            var mensaje = envioSiat.Transaccion
                ? envioSiat.Enviado
                    ? "Factura reenviada y validada por el SIAT."
                    : "La factura ya estaba validada por el SIAT."
                : "Factura reenviada al SIAT con observaciones o error de comunicación.";

            return Ok(new
            {
                message = mensaje,
                VentaId = id,
                Siat = envioSiat
            });
        }

        [HttpPut("liberar")]
        public async Task<IActionResult> Liberar()
        {
            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();

            if (paraLlevar is null)
                return NotFound(new { message = "Configuración para llevar no encontrada" });

            if (paraLlevar.Pedido is null)
                return BadRequest(new { message = "No hay un pedido para llevar activo" });

            if (paraLlevar.Pedido.Total > 0)
                return BadRequest(new { message = "No puedes liberar un pedido sin antes cobrar" });

            var tieneSubVentas = await _db.subventas.Query().AnyAsync(s => s.Id_Pedido == paraLlevar.Pedido.Id);

            if (!tieneSubVentas)
                await _db.Pedidos.Remove(paraLlevar.Pedido);

            paraLlevar.Disponible = true;
            paraLlevar.Pedido = null;

            await _db.SaveUnitWork();

            await _notificador.NotificarPedidoParaLlevarActualizado(
                new ParaLlevarPayload(IdPedido: null, Disponible: true));

            return Ok(new { message = "Pedido para llevar liberado" });
        }

        // ─── helper ──────────────────────────────────────────────────────────
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

        [HttpPost("reembolso/{Id:int}")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Reembolso(int Id, DtoReembolso datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Enum.IsDefined(typeof(TipoPagos), datos.TipoPago))
                return BadRequest(new { message = $"Tipo de pago inválido. Los valores permitidos son: {string.Join(", ", Enum.GetNames(typeof(TipoPagos)))}" });

            var venta = await _db.ventas.FindByIdAsync(Id);
            if (venta is null)
                return NotFound(new { message = "Venta no encontrada" });

            if (venta.EstadoSiat == FacturaEstado.Anulada)
                return BadRequest(new { message = "Esta venta ya fue reembolsada" });

            var caja = await _db.cajas.ObtenerCaja();
            if (caja is null)
                return BadRequest(new { message = "No hay una caja abierta" });

            if (datos.Monto > venta.MontoTotal)
                throw new VentaException("El monto a reembolsar no puede ser mayor al total de la venta");

            var movimiento = venta.Reembolso(caja, datos.Monto, datos.TipoPago, datos.Motivo);

            await _db.cajamovimientos.Crear(movimiento);
            await _db.SaveUnitWork();

            return Ok(new { message = "Reembolso procesado correctamente", venta.Cuf });
        }
    }
}
