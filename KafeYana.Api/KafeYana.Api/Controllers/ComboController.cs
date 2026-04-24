using KafeYana.Application.Dtos.ComboDtos;
using KafeYana.Application.Dtos.ElaboradosDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ComboController(IComboRepositorio _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoComboClient datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _db.ExisteAsync(x => x.Nombre == datos.Nombre))
                throw new CampoYaExistenteFailException(datos.Nombre);

            try
            {
                await datos.ValidarProductos(_db);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await _db.Crear(datos.Crear());

            await _db.SaveAsync();

            return Created();
        }


        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(int Id, DtoComboClient datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await datos.ValidarProductos(_db);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var producto = await _db.TraerProducto(Id: Id, combo: true);

            if (producto is null || producto.Promocion is null || producto.Promocion.Detalles is null) return BadRequest("Producto elaborado no existe");

            if (datos.Nombre != producto.Nombre && await _db.ExisteAsync(x => x.Nombre == datos.Nombre)) throw new CampoYaExistenteFailException(datos.Nombre);

            datos.Actualizar(producto);

            await _db.SaveAsync();

            return NoContent();
        }

    }
}
