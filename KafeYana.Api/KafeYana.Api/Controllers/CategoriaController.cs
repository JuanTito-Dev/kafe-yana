using KafeYana.Application.Dtos.Categoria;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Inventario;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaController(ICategoriaRepositorio _Categoria) : ControllerBase
    {

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Crear(DtoCategoriaCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
                
            try
            {
                var Categoria = datos.Adapt<Categoria>();

                await _Categoria.Crear(Categoria);

                await _Categoria.SaveAsync();

                return Ok(Categoria.Adapt<DtoCategoriaCrear>());
            }
            catch
            {
                return Conflict(new { mensaje = $"Ya existe una categoría con ese nombre" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int Id, DtoCategoriaUpdate datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var categoria = await _Categoria.FindByIdAsync(Id);

            if (categoria == null) return BadRequest(new { message = "Categoria no encontrada" });

            if(categoria.Nombre != datos.Nombre)
            {
                if (await _Categoria.ExisteAsync(x => x.Nombre == datos.Nombre))
                {
                    throw new CampoYaExistenteFailException(datos.Nombre);
                }
            }

            datos.Adapt(categoria);

            await _Categoria.SaveAsync();

            return Ok(categoria.Adapt<DtoCategoriaUpdate>());
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            if (Id == 23) return BadRequest(new { message = "No puedes eliminar la categoria combos" });

            var categoria = await _Categoria.BuscarConProductos(Id);

            if (categoria.Productos.Any())
                throw new InventarioException("No se puede eliminar la categoría porque tiene productos asociados");

            await _Categoria.Remove(categoria);

            await _Categoria.SaveAsync();

            return NoContent();
        }
    }
}
