
using KafeYana.Application.Dtos.VariacionesDtos; 
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}")]
    public class VariacionController(IVariacionReposiotorio _db, IUnitWork _base) : ControllerBase
    {
        [HttpPost("Variacion")]
        public async Task<IActionResult> Crear(DtoVariacionCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.TraerProducto(datos.Id_Producto, elaborado: true);

            if (producto == null) return NotFound(new { message = "Producto no encontrado" });

            if (producto.Elaborado is null) return NotFound(new { message = "Prodcuto no encontrado" });

            if (producto.Elaborado.Producible) return BadRequest(new { message = "Este producto no puede tener varciaiones" });

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

            if (producto == null || Id < 0) return NotFound(new { message = "Producto no encontrado" });

            if (producto.Elaborado is null) return NotFound(new { message = "Prodcuto no encontrado" });

            if (producto.Elaborado.Producible) return BadRequest(new { message = "Este producto no puede tener varciaiones" });

            datos.Id_Producto = producto.Elaborado.Id;

            var avacion = await _db.FindByIdAsync(Id);

            if (avacion == null) return NotFound(new { message = "Variacion no encontrada" });

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
            var variacion = await _db.FindByIdAsync(Id);
            if (variacion == null) return BadRequest(new { message = "Variacion no encontrada" });
            await _db.Remove(variacion);
            await _db.SaveAsync();
            return Ok(new {message = "variacion eliminada"});
        }


        [HttpPost("Opcion")]
        public async Task<IActionResult> AgregarOpcion(DtoOpcionCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var variacion = await _base.variaciones.FindByIdAsync(datos.Id_variacion);

            if (variacion == null) return NotFound(new { message = "Variacion no encontrada" });

            var receta = await _base.recetas.GetRectaByIdElaborado(variacion.Id_Elaborado);

            if (receta == null) return NotFound(new { message = "Receta no encontrada" });

            var Variacionnuevo = datos.Crear(receta, datos.Id_variacion);

            if (Variacionnuevo is null) return BadRequest(new { message = "Un insumo nose encuentra en la receta" });

            await _base.variaciones.CrearOpcion(Variacionnuevo);

            await _base.SaveUnitWork();

            return Created("", new {message = "Opcion creada"});
        }

        [HttpPut("Opcion/{Id:int}")]
        public async Task<IActionResult> UpdateOpcion(DtoOpcionCU datos, int Id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var variacion = await _base.variaciones.FindByIdAsync(datos.Id_variacion);

            if (variacion == null) return NotFound(new { message = "Variacion no encontrada" });

            var receta = await _base.recetas.GetRectaByIdElaborado(variacion.Id_Elaborado);

            if (receta == null) return NotFound(new { message = "Receta no encontrada" });

            var opcion = await _base.opciones.TraerOpcion(Id);

            if (opcion == null) return BadRequest(new { message = "Opcion no encontrada" });

            if(!datos.Actualizar(receta,opcion)) return BadRequest(new { message = "Un insumo nose encuentra en la receta" });

            await _db.SaveAsync();

            return Ok(new {message = "Opcion actualizada"});
        }

        [HttpDelete("Opcion/{Id:int}")]
        public async Task<IActionResult> DeleteOpcion(int Id)
        {

            var opcion = await _db.TraerOpcion(Id);

            if (opcion == null) return NotFound(new { message = "No encontrado" });

            await _db.DeleteOpcion(opcion);

            await _db.SaveAsync();

            return Ok(new {message = "Opcion eliminada"});
        }
    }
}
