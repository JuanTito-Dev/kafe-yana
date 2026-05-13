using KafeYana.Application.Dtos.OrdenDeCompraDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}")]
    public class OrdenCompraController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoOrdenCompra datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (datos.Insumos.Count == 0 && datos.Productos.Count == 0)
                return BadRequest(new { message = "Debe agregar al menos un item a la orden" });

            var proveedor = await _db.proveedores.FindByIdAsync(datos.Id_Proveedor);
            if (proveedor is null)
                return NotFound(new { message = "Proveedor no encontrado" });

            var itemsInsumos = new List<OrdenItemInsumo>();
            var itemsProductos = new List<OrdenItemProducto>();
            var total = 0.00M;

            foreach (var item in datos.Insumos)
            {
                var insumo = await _db.insumos.FindByIdAsync(item.Id_Insumo);
                if (insumo is null)
                    return NotFound(new { message = $"Insumo con ID {item.Id_Insumo} no encontrado" });

                var subtotal = item.Cantidad * item.Precio;
                total += subtotal;

                itemsInsumos.Add(new OrdenItemInsumo
                {
                    Id_Insumo = item.Id_Insumo,
                    Nombre = insumo.Nombre,
                    Cantidad = item.Cantidad,
                    Precio = item.Precio,
                    Subtotal = subtotal
                });
            }

            foreach (var item in datos.Productos)
            {
                var producto = await _db.productos.FindByIdAsync(item.Id_Producto);
                if (producto is null)
                    return NotFound(new { message = $"Producto con ID {item.Id_Producto} no encontrado" });

                if (producto.Tipo != TiposProductos.Comprado) return NotFound(new { message = "Producto no es comprado" });

                var subtotal = item.Cantidad * item.Precio;
                total += subtotal;

                itemsProductos.Add(new OrdenItemProducto
                {
                    Id_Producto = item.Id_Producto,
                    Nombre = producto.Nombre,
                    Cantidad = item.Cantidad,
                    Precio = item.Precio,
                    Subtotal = subtotal
                });
            }

            var CodigoDb = await _db.ordenes.Count() + 1;

            var orden = new OrdenCompra
            {
                Codigo = $"OC-{CodigoDb:D3}",
                Nombre_Proveedor = proveedor.Razon_Social,
                Id_Proveedor = datos.Id_Proveedor,
                Fecha = datos.FechaEntrega,
                Total = total,
                Nota = datos.Nota,
                Insumos = itemsInsumos,
                Productos = itemsProductos
            };

            await _db.ordenes.Crear(orden);
            await _db.SaveUnitWork();

            return Ok(new { message = "Orden de compra creada correctamente", Id = orden.Id });
        }


        [HttpPut("recibir/{Id:int}")]
        public async Task<IActionResult> RecibirOrden(int Id)
        {
            var orden = await _db.ordenes.TraerOrdenCompleta(Id);
            if (orden is null)
                return NotFound(new { message = "Orden no encontrada" });

            if (orden.Recibido)
                return BadRequest(new { message = "Esta orden ya fue recibida" });

            foreach (var item in orden.Insumos)
            {
                if (item.Insumo is null)
                    throw new OrdenCompraException($"Insumo no encontrado: {item.Id_Insumo}");

                var movimiento = item.Insumo.Compra(orden.Codigo, item.Cantidad, item.Precio);
                await _db.Insumomovientos.Crear(movimiento);
            }

            foreach (var item in orden.Productos)
            {
                if (item.Producto?.Comprado is null)
                    throw new OrdenCompraException($"Producto comprado no encontrado: {item.Id_Producto}");

                var movimiento = item.Producto.Comprado.Compra(orden.Codigo, item.Cantidad, item.Precio);
                await _db.movimientos.Crear(movimiento);
            }

            orden.Recibir();
            orden.CortarRelaciones();

            await _db.SaveUnitWork();

            return Ok(new { message = "Orden recibida correctamente", orden.Codigo });
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            var orden = await _db.ordenes.TraerOrdenCompleta(Id);

            if (orden is null)
                return NotFound(new { message = "Orden no encontrada" });

            if (orden.Recibido)
                return BadRequest(new { message = "No puede cancelar esta orden ya fue recibida" });

            orden.Cancelar();
            orden.CortarRelaciones();

            await _db.SaveUnitWork();

            return Ok(new { message = "Orden cancelada correctamente" });
        }

    }
}
