using KafeYana.Application.Dtos.ProveedorDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedorController(IProveedorRepositorio _proveedor) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Crear(DtoProveedorCrear datos)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var proveedor = datos.Adapt<Proveedor>();
            await _proveedor.Crear(proveedor);
            await _proveedor.SaveAsync();

            return Created();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoProveedorUpdate datos)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var proveedor = await _proveedor.FindByIdAsync(id);
            if (proveedor == null)
                return NotFound(new { message = "Proveedor no encontrado" });

            datos.Adapt(proveedor);
            await _proveedor.SaveAsync();

            return Ok(new { message = "Proveedor actualizado" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var proveedor = await _proveedor.FindByIdAsync(id);
            if (proveedor == null)
                return NotFound(new { message = "Proveedor no encontrado" });

            await _proveedor.Remove(proveedor);
            await _proveedor.SaveAsync();

            return NoContent();
        }
    }
}