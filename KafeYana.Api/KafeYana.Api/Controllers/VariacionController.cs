
using KafeYana.Application.Dtos.VariacionesDtos; 
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
    public class VariacionController(IVariacionReposiotorio _db) : ControllerBase
    {
        [HttpPost("Variacion")]
        public async Task<IActionResult> Crear(DtoVariacionCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.TraerProducto(datos.Id_Producto, elaborado: true);

            if (producto == null) return BadRequest("Producto no encontrado");

            datos.Id_Producto = producto.Elaborado.Id;

            var variacion = datos.Crear();

            await _db.Crear(variacion);

            await _db.SaveAsync();

            return Created("variacion", new
            {
                variacion.Id,
                variacion.Nombre,
                variacion.Requerido
            });
        }

        [HttpPut("Variacion/{Id:int}")]
        public async Task<IActionResult> Update(DtoVariacionCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.TraerProducto(datos.Id_Producto, elaborado: true);

            if (producto == null || Id < 0) return BadRequest("Producto no encontrado");

            datos.Id_Producto = producto.Elaborado.Id;

            var avacion = await _db.FindByIdAsync(Id);

            if (avacion == null) return BadRequest("Variacion no encontrada");

            datos.Actualizar(avacion);

            await _db.SaveAsync();

            return Created("variacion", new
            {
                avacion.Id,
                avacion.Nombre,
                avacion.Requerido
            });

        }

        [HttpDelete("Variacion/{Id:int}")]
        public async Task<IActionResult> Delete(int Id)
        {
            if (Id < 0) return BadRequest("Id no valido");
            var variacion = await _db.FindByIdAsync(Id);
            if (variacion == null) return BadRequest("Variacion no encontrada");
            await _db.Remove(variacion);
            await _db.SaveAsync();
            return NoContent();
        }


        [HttpPost("Opcion")]
        public async Task<IActionResult> AgregarOpcion(DtoOpcionCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _db.Existe(datos.Id_variacion)) return BadRequest("Variacion no encontrada");

            var Variacion = datos.Crear();

            await _db.CrearOpcion(Variacion);

            await _db.SaveAsync();

            return Created();
        }

        [HttpPut("Opcion/{Id:int}")]
        public async Task<IActionResult> UpdateOpcion(DtoOpcionCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var opcion = await _db.TraerOpcion(Id);

            if (opcion == null) return BadRequest("Opcion no encontrada");

            datos.Actualizar(opcion);

            await _db.SaveAsync();

            return NoContent();
        }

        [HttpDelete("Opcion/{Id:int}")]
        public async Task<IActionResult> DeleteOpcion(int Id)
        {
            if (Id < 0 || !await _db.ExisteOpcion(Id)) return BadRequest("Opcion no encontrada");

            var opcion = await _db.TraerOpcion(Id);

            if (opcion == null) return BadRequest("No encontrado");

            await _db.DeleteOpcion(opcion);

            await _db.SaveAsync();

            return NoContent();
        }
    }
}
