using KafeYana.Application.Dtos.Categoria;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = $"{RolesKafe.Admin}")]
    public class CategoriaController(ICategoriaRepositorio _Categoria) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> Crear(DtoCategoriaCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var Categoria = datos.Adapt<Categoria>();

            await _Categoria.Crear(Categoria);

            await _Categoria.SaveAsync();

            return Created("", new { message = "Categoria creada"});
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int Id, DtoCategoriaUpdate datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var categoria = await _Categoria.FindByIdAsync(Id);

            if (categoria == null) return NotFound(new { message = "Categoria no encontrada" });

            datos.Adapt(categoria);

            await _Categoria.SaveAsync();

            return Ok(new { message = "Categoria actualizada" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            if (Id == 3) return BadRequest(new { message = "No puedes eliminar la categoria combos" });

            var categoria = await _Categoria.FindByIdAsync(Id);

            if (categoria == null) return NotFound(new { message = "Categoria no encontrada" });

            await _Categoria.Remove(categoria);

            await _Categoria.SaveAsync();

            return Ok(new { message = "Categoria eliminada" });
        }
    }
}
