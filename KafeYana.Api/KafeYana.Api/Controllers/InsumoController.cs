using KafeYana.Application.Dtos.Insumos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsumoController(IInsumoRepositorio _db) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Crear(DtoInsumosCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nombre = await _db.ExisteAsync(x => x.Nombre == datos.Nombre);

            if (nombre) throw new CampoYaExistenteFailException(datos.Nombre);

            var insumo = datos.Adapt<Insumo>();

            await _db.Crear(insumo);

            await _db.SaveAsync();

            return Created();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{Id:int}")]

        public async Task<IActionResult> Update(DtoInsumosCrear datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var insumodb = await _db.FindByIdAsync(Id);

            if (insumodb == null) return BadRequest("Insumo no encontrado");

            if (datos.Nombre != insumodb.Nombre && await _db.ExisteAsync(x => x.Nombre == datos.Nombre)) throw new CampoYaExistenteFailException(datos.Nombre);

            datos.Adapt(insumodb);

            await _db.SaveAsync();

            return NoContent();

        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            if (Id <= 0) return BadRequest("Insumo no encontrado");

            var insumo = await _db.FindByIdAsync(Id);

            if (insumo is null) return BadRequest("Insumo no encontrado");

            await _db.Remove(insumo);

            await _db.SaveAsync();

            return NoContent();
        }
    }
}
