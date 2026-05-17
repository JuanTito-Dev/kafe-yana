using KafeYana.Application.Dtos.ProductoCanjeable;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RolesKafe.Admin)]
    public class ProductoCanjeableController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoProductoCanjeableCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!datos.Activo)
                return BadRequest(new { message = "No se puede crear un producto canjeable inactivo" });

            var producto = await _db.productos.Query()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == datos.Id_Producto);

            if (producto is null)
                throw new InventarioException("Producto no encontrado");

            var canjeable = new ProductoCanjeable
            {
                Id_Producto    = datos.Id_Producto,
                NombreProducto = producto.Nombre,
                Categoria      = producto.Categoria.Nombre,
                Puntos         = datos.Puntos,
                Disponible     = datos.Disponible,
                Activo         = datos.Activo
            };

            await _db.productosCanjeables.Crear(canjeable);
            await _db.SaveUnitWork();

            return Created("", new { message = "Producto canjeable creado", canjeable.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoProductoCanjeableCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canjeable = await _db.productosCanjeables.FindByIdAsync(id);
            if (canjeable is null)
                return NotFound(new { message = "Producto canjeable no encontrado" });

            // Si cambia el producto, refrescar nombre y categoría
            if (canjeable.Id_Producto != datos.Id_Producto)
            {
                var producto = await _db.productos.Query()
                    .Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == datos.Id_Producto);

                if (producto is null)
                    throw new InventarioException("Producto no encontrado");

                canjeable.Id_Producto    = datos.Id_Producto;
                canjeable.NombreProducto = producto.Nombre;
                canjeable.Categoria      = producto.Categoria.Nombre;
            }

            canjeable.Puntos     = datos.Puntos;
            canjeable.Disponible = datos.Disponible;
            canjeable.Activo     = datos.Activo;

            await _db.SaveUnitWork();

            return Ok(new { message = "Producto canjeable actualizado" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var canjeable = await _db.productosCanjeables.FindByIdAsync(id);
            if (canjeable is null)
                return NotFound(new { message = "Producto canjeable no encontrado" });

            await _db.productosCanjeables.Remove(canjeable);
            await _db.SaveUnitWork();

            return Ok(new { message = "Producto canjeable eliminado" });
        }
    }
}
