using KafeYana.Application.Dtos.HitoCompraDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RolesKafe.Admin)]
    public class HitoCompraController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoHitoCompraCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await EnsureProductoCanjeableExists(datos.Id_ProductoCanjeable);

            var hito = new HitoCompra
            {
                NumeroCompras         = datos.NumeroCompras,
                Id_ProductoCanjeable  = datos.Id_ProductoCanjeable,
                Descripcion           = datos.Descripcion,
                Icono                 = datos.Icono,
                Activo                = datos.Activo
            };

            await _db.hitosCompra.Crear(hito);
            await _db.SaveUnitWork();

            return Created("", new { message = "Hito por compra creado", hito.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoHitoCompraCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await EnsureProductoCanjeableExists(datos.Id_ProductoCanjeable);

            var hito = await _db.hitosCompra.FindByIdAsync(id);
            if (hito is null)
                return NotFound(new { message = "Hito por compra no encontrado" });

            hito.NumeroCompras        = datos.NumeroCompras;
            hito.Id_ProductoCanjeable = datos.Id_ProductoCanjeable;
            hito.Descripcion          = datos.Descripcion;
            hito.Icono                = datos.Icono;
            hito.Activo               = datos.Activo;

            await _db.SaveUnitWork();

            return Ok(new { message = "Hito por compra actualizado" });
        }

        /// <summary>Alterna Activo: si está activo lo desactiva y viceversa.</summary>
        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> AlternarActivo(int id)
        {
            var hito = await _db.hitosCompra.FindByIdAsync(id);
            if (hito is null)
                return NotFound(new { message = "Hito por compra no encontrado" });

            hito.Activo = !hito.Activo;
            await _db.SaveUnitWork();

            return Ok(new { message = "Estado actualizado", Activo = hito.Activo });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var hito = await _db.hitosCompra.FindByIdAsync(id);
            if (hito is null)
                return NotFound(new { message = "Hito por compra no encontrado" });

            await _db.hitosCompra.Remove(hito);
            await _db.SaveUnitWork();

            return Ok(new { message = "Hito por compra eliminado" });
        }

        private async Task EnsureProductoCanjeableExists(int id)
        {
            if (!await _db.productosCanjeables.Existe(id))
                throw new InventarioException($"Producto canjeable no encontrado (Id {id})");
        }
    }
}
