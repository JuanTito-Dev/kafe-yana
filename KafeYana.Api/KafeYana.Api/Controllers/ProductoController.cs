using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}")]
    public class ProductoController(IProductoRepositorio _producto, IProductoImagenService _imagenService) : ControllerBase
    {

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Crear([FromForm] DtoCompradoCrear datos, IFormFile? Imagen)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = datos.ProductoCrear();

            if (Imagen is not null && Imagen.Length > 0)
                producto.UrlImagen = await _imagenService.ProcesarSubidaAsync(Imagen, datos.Nombre, datos.Categoria_Id);

            await _producto.Crear(producto);
            await _producto.SaveAsync();

            return Created("", new { message = "Producto creado" });
        }

        [HttpPut("{Id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int Id, [FromForm] DtoCompradoCrear datos, IFormFile? Imagen)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var productoDb = await _producto.TraerProducto(Id, comprado: true);

            if (productoDb is null || productoDb.Tipo != TiposProductos.Comprado)
                return NotFound(new { message = "Producto no encontrado" });

            datos.Editar(productoDb);

            if (Imagen is not null)
            {
                await _imagenService.EliminarSiExisteAsync(productoDb.UrlImagen);
                productoDb.UrlImagen = await _imagenService.ProcesarSubidaAsync(Imagen, datos.Nombre, datos.Categoria_Id);
            }

            await _producto.SaveAsync();

            return Ok(new { message = "Producto actualizado" });
        }

        [HttpPut("{Id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update2(int Id, [FromForm] DtoCompradoCrear datos, IFormFile? Imagen)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var productoDb = await _producto.TraerProducto(Id, comprado: true);

            if (productoDb is null || productoDb.Tipo != TiposProductos.Comprado)
                return NotFound(new { message = "Producto no encontrado" });

            datos.Editar(productoDb);

            if (Imagen is not null)
            {
                await _imagenService.EliminarSiExisteAsync(productoDb.UrlImagen);
                productoDb.UrlImagen = await _imagenService.ProcesarSubidaAsync(Imagen, datos.Nombre, datos.Categoria_Id);
            }

            await _producto.SaveAsync();

            return Ok(new { message = "Producto actualizado" });
        }

        [HttpDelete("{Id:int}")]
        public async Task<IActionResult> Eliminar(int Id)
        {
            var producto = await _producto.FindByIdAsync(Id);

            if (producto is null) return NotFound(new { message = "Producto no encontrado" });

            await _imagenService.EliminarSiExisteAsync(producto.UrlImagen);

            await _producto.Remove(producto);
            await _producto.SaveAsync();

            return Ok(new { message = "Producto eliminado" });
        }
    }
}
