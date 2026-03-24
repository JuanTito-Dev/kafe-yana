using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProductoController(IProductoRepositorio _producto) : ControllerBase
    {
        
        [HttpPost]
        public async Task<IActionResult> Crear(DtoCompradoCrear datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            await _producto.Crear(datos.ProductoCrear());
            await _producto.SaveAsync();

            return Ok();
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(int Id, DtoCompradoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var productoDb = await _producto.TraerProducto(Id, comprado: true);

            if (productoDb is null) return BadRequest();

            datos.Editar(productoDb);

            await _producto.SaveAsync();

            return NoContent();
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            var producto = await _producto.FindByIdAsync(Id);

            if (producto is null) return BadRequest();

            await _producto.Remove(producto);

            await _producto.SaveAsync();

            return Ok();

        }
    }
}
