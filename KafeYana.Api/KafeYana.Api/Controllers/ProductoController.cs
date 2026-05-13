using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.Exceptions;
using KafeYana.Domain.Entities.Inventario;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}")]
    public class ProductoController(IProductoRepositorio _producto) : ControllerBase
    {
        
        [HttpPost]
        public async Task<IActionResult> Crear(DtoCompradoCrear datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            await _producto.Crear(datos.ProductoCrear());
            await _producto.SaveAsync();

            return Created("", new { message = "Producto creado"} );
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(int Id, DtoCompradoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var productoDb = await _producto.TraerProducto(Id, comprado: true);

            if (productoDb is null || productoDb.Tipo != TiposProductos.Comprado) return NotFound(new { message = "Producto no encontrado" });  

            datos.Editar(productoDb);

            await _producto.SaveAsync();

            return Ok(new { message = "Prodcutos actualizado"});
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            var producto = await _producto.FindByIdAsync(Id);

            if (producto is null) return NotFound(new { message = "Producto no encontrado"});

            await _producto.Remove(producto);

            await _producto.SaveAsync();

            return Ok(new { message = "Prodcuto eliminado"});

        }
    }
}
