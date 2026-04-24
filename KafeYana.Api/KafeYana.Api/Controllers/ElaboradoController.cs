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
        public async Task<IActionResult> Crear(DtoElaboradoCliente datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            if(await _repo.ExisteAsync(x => x.Nombre == datos.Nombre)) throw new CampoYaExistenteFailException(datos.Nombre);

            await _repo.Crear(datos.CrearEntidad());

            await _repo.SaveAsync();

            return Created();
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Update(int Id, DtoElaboradoCliente datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _repo.TraerProducto(Id: Id, elaborado: true);

            if (producto is null || producto.Elaborado is null) return BadRequest("Producto elaborado no existe");

            if (datos.Nombre != producto.Nombre && await _repo.ExisteAsync(x => x.Nombre == datos.Nombre)) throw new CampoYaExistenteFailException(datos.Nombre);


            datos.Editar(producto);

            await _repo.SaveAsync();

            return NoContent();
        }

    }
}
