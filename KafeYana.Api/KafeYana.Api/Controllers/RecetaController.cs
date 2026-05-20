using KafeYana.Application.Dtos.RecetaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class RecetaController(IRecetaRepositorio _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoRecetaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.TraerProducto(datos.Id_Elaborado, elaborado: true);

            if (producto == null || producto.Elaborado == null) return NotFound(new { message = "Producto no encontrado o no pertenece a elaborados" });

            datos.Id_Elaborado = producto.Elaborado.Id;

            var receta = datos.Adatar();

            await _db.Crear(receta);
            await _db.SaveAsync();
            return Created("", new { message = "Receta creada" });
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(DtoRecetaCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var Receta = await _db.GetReceta(Id);

            if (Receta == null) return NotFound(new { message = "Receta no encontrada" });

            var producto = await _db.TraerProducto(datos.Id_Elaborado, elaborado: true);

            if (producto == null || producto.Elaborado == null) return NotFound(new { message = "Producto no encontrado o no pertenece a elaborados" });

            datos.Id_Elaborado = producto.Elaborado.Id;

            datos.Actualizar(Receta);

            await _db.SaveAsync();

            return Ok(new { message = "Receta editada" });
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var receta = await _db.FindByIdAsync(Id);
            if (receta is null) return NotFound(new { message = "Receta no encontrada" });
            await _db.Remove(receta);
            await _db.SaveAsync();
            return Ok(new {message = "Receta eliminada"});
        }
    }
}
