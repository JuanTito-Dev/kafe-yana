using KafeYana.Application.Dtos.Insumos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class InsumoController(IInsumoRepositorio _db) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Crear(DtoInsumosCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var insumo = datos.Adapt<Insumo>();

            await _db.Crear(insumo);

            await _db.SaveAsync();

            return Created("", new { message = "Insumo creado"});
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{Id:int}")]

        public async Task<IActionResult> Update(DtoInsumosCrear datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var insumodb = await _db.FindByIdAsync(Id);

            if (insumodb == null) return NotFound(new { message = "Insumo no encontrado" });

            datos.Adapt(insumodb);

            await _db.SaveAsync();

            return Ok(new { message = "Insumo actualizado" } );

        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var insumo = await _db.FindByIdAsync(Id);

            if (insumo is null) return NotFound(new { message = "Insumo no encontrado" });

            await _db.Remove(insumo);

            await _db.SaveAsync();

            return Ok(new { message = "Insumo eliminado" });
        }
    }
}
