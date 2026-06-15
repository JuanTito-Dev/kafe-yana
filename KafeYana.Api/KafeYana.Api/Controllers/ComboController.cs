using KafeYana.Application.Dtos.ComboDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = $"{RolesKafe.Admin}")]
    public class ComboController(IComboRepositorio _db, IProductoImagenService _imagenService) : ControllerBase
    {
        private const int CategoriaComboId = 3;

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Crear([FromForm] DtoComboClient datos, IFormFile? Imagen)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(datos.CodigoSin))
                return BadRequest(new { message = "CodigoSin es requerido" });

            try
            {
                await datos.ValidarProductos(_db);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var producto = datos.Crear();

            if (Imagen is not null && Imagen.Length > 0)
                producto.UrlImagen = await _imagenService.ProcesarSubidaAsync(Imagen, datos.Nombre, CategoriaComboId);

            await _db.Crear(producto);
            await _db.SaveAsync();

            producto.AsignarCodigo(ProductoCodigoService.Generar(producto.Id));
            await _db.SaveAsync();

            return Created("", new { message = "Combo creado", Id = producto.Id, Codigo = producto.Codigo });
        }

        [HttpPut("{Id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int Id, [FromForm] DtoComboClient datos, IFormFile? Imagen)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(datos.CodigoSin))
                return BadRequest(new { message = "CodigoSin es requerido" });

            try
            {
                await datos.ValidarProductos(_db);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var producto = await _db.TraerProducto(Id: Id, combo: true);

            if (producto is null || producto.Promocion is null || producto.Promocion.Detalles is null)
                return NotFound("Combo no existe");

            datos.Actualizar(producto);

            if (Imagen is not null)
            {
                await _imagenService.EliminarSiExisteAsync(producto.UrlImagen);
                producto.UrlImagen = await _imagenService.ProcesarSubidaAsync(Imagen, datos.Nombre, CategoriaComboId);
            }

            await _db.SaveAsync();

            return Ok(new { message = "Combo actualizado" });
        }
    }
}
