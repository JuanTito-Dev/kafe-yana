using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentaController(IUnitWork _db) : ControllerBase
    {
        //[HttpPost()]
        //public async Task<IActionResult> Venta(DtoVentaPedido datos)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    var nombre = User.Identity?.Name;

        //    if (nombre is null) return Unauthorized(new { message = "Usuario no identificado" });

        //    var pedido = await _db.Pedidos.TraerPedido(datos.Id_Pedido); 

        //    if (pedido == null) return NotFound("Pedido no encontrado");

        //    var cliente = pedido.Cliente;

        //    if (pedido.Id_Cliente == null || pedido.Id_Cliente != datos.Id_Cliente)
        //    {
        //        cliente = await _db.clientes.FindByIdAsync(datos.Id_Cliente);

        //        if(cliente == null) return NotFound("Cliente no encontrado");
        //    }

        //    int productoscant = 0;

        //    var detalle_venta = new List<Detalle_venta>();
        //    var subtotal = 0.00m;

        //    foreach (var ronda in pedido.Rondas)
        //    {
        //        foreach (var item in ronda.Detalle)
        //        {
        //            if (item.producto == null) return BadRequest($" {item.Id_Producto } Producto no encontrado");

        //            switch (item.producto.Tipo)
        //            {
        //                case TiposProductos.Comprado:
        //                    await DescontarComprado(item.Id_Producto, item.Cantidad);
        //                    break;

        //                case TiposProductos.Elaborado:
        //                    await DescontarElaborado(item.Id_Producto, item.Cantidad, item.Id_Opcion);
        //                    break;

        //                case TiposProductos.Promocion:

        //                    break;
        //            }
        //            productoscant++;

        //            detalle_venta.Add(new Detalle_venta
        //            {
        //                Nombre = item.producto.Nombre,
        //                Cantidad = item.Cantidad,
        //                Precio = item.Precio,
        //                Total = item.Precio * item.Cantidad
        //            });

        //            subtotal += item.Precio * item.Cantidad;
        //        }
        //    }

        //    var venta = new Venta
        //    {
        //        Fecha = DateTime.Now,
        //        Cliente = cliente!.Nombre,
        //        Cajero = nombre,
        //        Productos = productoscant,
        //        Pago = datos.TipoPago.ToString(),
        //        Estado = "Finalizada",
        //        Subtotal = subtotal,
        //        Total = subtotal,// Aquí podrías aplicar descuentos o impuestos si es necesario
        //        Detalles = detalle_venta
        //    };

        //    return Ok(new { message = "Venta realizada con éxito", venta });
        //}

        //public async Task DescontarComprado(int Id, int cantidad)
        //{
        //    var producto = await _db.productos.TraerProducto(Id, comprado: true);

        //    if (producto is null) throw new InventarioException($"Producto no encontrado {Id}");

        //    if (producto.Comprado.Stock_actual < cantidad) throw new InventarioException($"Stock insuficiente para el producto {producto.Nombre}");

        //    producto.Comprado.Stock_actual -= cantidad;

        //}

        //public async Task DescontarElaborado(int Id, int Cantidad, int? Id_Opcion)
        //{
        //    if (await _db.elaborados.EsProducible(Id))
        //    {
        //        var elaborado = await _db.elaborados.TraerElaborado(Id);
        //        if (elaborado is null) throw new InventarioException($"Elaborado no encontrado {Id}");
        //        elaborado.Stock_actual -= Cantidad;
        //    }
        //    else
        //    {
        //        var elaborado = await _db.elaborados.TraerElaborado(Id, withreceta: true);
        //        if (elaborado is null) throw new InventarioException($"Elaborado no encontrado {Id}");
        //        if (elaborado.Receta is null) return; // Sin receta, no se hace nada

        //        var opcion = Id_Opcion is not null ? await _db.opciones.TraerOpcion(Id_Opcion.Value) : null;

        //        // Insumos que son REEMPLAZO - se omiten de la receta
        //        var insumoOmitidos = opcion?.Ajustes
        //            .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
        //            .Select(x => x.Id_Insumo)
        //            .ToHashSet() ?? new HashSet<int>();

        //        foreach (var detalle in elaborado.Receta.Detalles)
        //        {
        //            // Si este insumo fue reemplazado, se omite
        //            if (insumoOmitidos.Contains(detalle.Id_insumo)) continue;

        //            var cantidadFinal = detalle.Cantidad * Cantidad * (1 + (detalle.Merma / 100));

        //            // Si tiene ajuste de MODIFICACION sobre este insumo
        //            var ajuste = opcion?.Ajustes
        //                .FirstOrDefault(x => x.Id_Insumo == detalle.Id_insumo
        //                                  && x.TipoAjuste == TiposAjuste.Modificacion);

        //            if (ajuste is not null)
        //            {
        //                cantidadFinal += ajuste.Cantidad * Cantidad; // puede ser + o -
        //            }

        //            detalle.Insumo.Stock_actual -= (int)cantidadFinal;
        //        }

        //        // Descontar los insumos NUEVOS de los reemplazos
        //        if (opcion is not null)
        //        {
        //            var reemplazos = opcion.Ajustes.Where(x => x.TipoAjuste == TiposAjuste.Reemplazo);
        //            foreach (var ajuste in reemplazos)
        //            {
        //                // InsumoNuevo es el que reemplaza al de la receta
        //                ajuste.InsumoNuevo.Stock_actual -= (int)(ajuste.Cantidad * Cantidad);
        //            }
        //        }
        //    }
        //}

        //public async Task DescontarPromocion(int Id, int Cantidad)
        //{
        //    var promocion = await _db.Combo.TraerPromocion(Id); // con Detalles e Include producto
        //    if (promocion is null) throw new InventarioException($"Promocion no encontrada {Id}");

        //    foreach (var detalle in promocion.Detalles)
        //    {
        //        if (detalle.Producto is null) throw new InventarioException($"Producto no encontrado en promocion {Id}");

        //        var cantidadTotal = detalle.Cantidad * Cantidad; // cantidad del detalle x cantidad pedida

        //        switch (detalle.Producto.Tipo)
        //        {
        //            case TiposProductos.Comprado:
        //                await DescontarComprado(detalle.Id_Producto, cantidadTotal);
        //                break;

        //            case TiposProductos.Elaborado:
        //                await DescontarElaborado(detalle.Id_Producto, cantidadTotal, null);
        //                // null porque una promocion no tiene opcion por producto
        //                break;
        //        }
        //    }
        //}
    }
}
