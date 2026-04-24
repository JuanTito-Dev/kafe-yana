using KafeYana.Application.Dtos.RecetaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RecetaController(IRecetaRepositorio _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoRecetaCU datos)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                if (await _db.ExisteAsync(x => x.Nombre == datos.Nombre))
                    throw new CampoYaExistenteFailException(datos.Nombre);

                var producto = await _db.TraerProducto(datos.Id_Elaborado, elaborado: true);

                if (producto == null || producto.Elaborado == null) return NotFound("Producto no encontrado o no pertenece a elaborados");

                datos.Id_Elaborado = producto.Elaborado.Id;

                var receta = datos.Adatar();

                await _db.Crear(receta);
                await _db.SaveAsync();
                return Created();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                Console.WriteLine($"Inner2: {ex.InnerException?.InnerException?.Message}");
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(DtoRecetaCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _db.Existe(Id)) return NotFound("Receta no encontrada");

            var Receta = await _db.GetReceta(Id);

            if (Receta == null) return NotFound("Receta no encontrada");

            var producto = await _db.TraerProducto(datos.Id_Elaborado, elaborado: true);

            if (producto == null || producto.Elaborado == null) return NotFound("Producto no encontrado o no pertenece a elaborados");

            datos.Id_Elaborado = producto.Elaborado.Id;

            if (datos.Nombre != Receta.Nombre && await _db.ExisteAsync(x => x.Nombre == datos.Nombre)) throw new CampoYaExistenteFailException(datos.Nombre);


            datos.Actualizar(Receta);

            await _db.SaveAsync();

            return NoContent();
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            if (Id <= 0) return BadRequest("Receta no encontrada");
            var receta = await _db.FindByIdAsync(Id);
            if (receta is null) return NotFound("Receta no encontrada");
            await _db.Remove(receta);
            await _db.SaveAsync();
            return NoContent();
        }
    }
}
