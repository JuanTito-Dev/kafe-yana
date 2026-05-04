using KafeYana.Application.Dtos.ElaboradosDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ElaboradoController(IElaboradoRepositorio _repo) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoElaboradoCrear datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var entidad = datos.CrearEntidad();

            await _repo.Crear(entidad);

            await _repo.SaveAsync();

            return Created("", new
            {
                entidad.Id,
                entidad.Nombre,
                entidad.Precio,
                message = "Producto creado"
            });
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(int Id, DtoElaboradoActualizar datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _repo.TraerProducto(Id: Id, elaborado: true);

            if (producto is null || producto.Elaborado is null) return NotFound("Producto elaborado no existe");

            datos.Editar(producto);

            await _repo.SaveAsync();

            return Ok(new
            {
                producto.Id,
                producto.Nombre,
                producto.Precio,
                message = "Producto creado"
            });
        }

    }
}
