using KafeYana.Api.Filters;
using KafeYana.Api.Hubs;
using KafeYana.Application.Dtos.MesaDtos;
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
using System.Threading.Tasks;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]
    public class VentaController(IUnitWork _db, Detalle_RondaService _detalleRondaService, IVentaServices _venta, IKafeYanaNotificador _notificador) : ControllerBase
    {
        [HttpPost("pedido")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> CrearPedido(DtoniciarMesa datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();

            if (paraLlevar is null)
                return NotFound(new { message = "Configuración para llevar no encontrada" });

            if (!paraLlevar.Disponible)
                return BadRequest(new { message = "Ya existe un pedido para llevar activo" });

            var nuevoPedido = datos.Adapt<Pedido>();
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
            paraLlevar.Pedido.Total += ronda.SubTotal;

            await _db.SaveUnitWork();

            var rondaPayload = BuildRondaPayload("Para llevar", paraLlevar.Pedido.Id, ronda);

            await _notificador.NotificarNuevaRonda(rondaPayload);

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

        [HttpPost("cobrar")]
        [ServiceFilter(typeof(CajaAbiertaFilter))]
        public async Task<IActionResult> Cobrar(DtoVentaPedido datos)
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

            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return Unauthorized(new { message = "Usuario no identificado" });

            if (datos.Pagos.Total != (await _db.Pedidos.FindByIdAsync(datos.Id_Pedido))?.Total)
                return BadRequest(new { message = "El total de los pagos no coincide con el total del pedido." });

            var venta = await _venta.ProcesarVenta(datos.Id_Pedido, datos.Id_Cliente, nombreUsuario, datos.Pagos);

            await _db.ventas.Crear(venta);

            paraLlevar.Disponible = true;
            paraLlevar.Pedido = null;

            var caja = (Caja)HttpContext.Items["Caja"]!;
            caja.RegistrarVenta(datos.Pagos.Efectivo, datos.Pagos.Tarjeta, datos.Pagos.Qr);

            await _db.SaveUnitWork();

            await _notificador.NotificarVentaProcesada(
                new VentaPayload("Para llevar", datos.Id_Pedido, datos.Pagos.Total));
            await _notificador.NotificarPedidoParaLlevarActualizado(
                new ParaLlevarPayload(IdPedido: null, Disponible: true));

            return Ok(new { message = "Venta procesada correctamente" });
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

            var venta = await _db.ventas.FindByIdAsync(Id);
            if (venta is null)
                return NotFound(new { message = "Venta no encontrada" });

            if (venta.Estado == "Reembolsado")
                return BadRequest(new { message = "Esta venta ya fue reembolsada" });

            var caja = await _db.cajas.ObtenerCaja();
            if (caja is null)
                return BadRequest(new { message = "No hay una caja abierta" });


            if (datos.Monto > venta.Total)
                throw new VentaException($"El monto a reembolsar no puede ser mayor al total de la venta");

            var movimiento = venta.Reembolso(caja, datos.Monto, datos.Nota);

            await _db.cajamovimientos.Crear(movimiento);
            await _db.SaveUnitWork();

            return Ok(new { message = "Reembolso procesado correctamente", venta.Codigo });
        }
    }
}
