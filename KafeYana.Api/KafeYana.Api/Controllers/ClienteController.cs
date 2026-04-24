using KafeYana.Application.Dtos.ClienteDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController(IClienteRespositorio _clientes) : ControllerBase
    {
        [HttpPost]
        [Authorize (Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Crear(DtoClienteCU datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            // ✅ Secuencial — una query a la vez sobre el mismo DbContext
            if (await _clientes.ExisteAsync(x => x.Nombre == datos.Nombre))
                throw new CampoYaExistenteFailException(datos.Nombre);

            if (await _clientes.ExisteAsync(x => x.Celular == datos.Celular))
                throw new CampoYaExistenteFailException(datos.Celular);

            if (datos.Correo != null &&
                await _clientes.ExisteAsync(x => x.Correo == datos.Correo.ToLower()))
                throw new CampoYaExistenteFailException(datos.Correo);

            if (datos.Dni != null &&
                await _clientes.ExisteAsync(x => x.Dni == datos.Dni))
                throw new CampoYaExistenteFailException(datos.Dni.ToString()!);

            var cliente = datos.Adapt<Cliente>();
            cliente.Correonormalizado = (datos.Correo is not null && datos.Correo != string.Empty)
                ? datos.Correo.ToUpper()
                : string.Empty;

            await _clientes.Crear(cliente);

            await _clientes.SaveAsync();

            return Created();
        }

        [HttpPut("{Id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Editar(int Id, DtoClienteCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (Id <= 0) return BadRequest("Id Invalido");

            var cliente = await _clientes.FindByIdAsync(Id);
            if (cliente == null) return BadRequest(new { message = "Cliente no encontrado" });

            if (datos.Nombre != cliente.Nombre)
                if (await _clientes.ExisteAsync(x => x.Id != Id && x.Nombre == datos.Nombre))
                    throw new CampoYaExistenteFailException(datos.Nombre);

            if (datos.Celular != cliente.Celular)
                if (await _clientes.ExisteAsync(x => x.Id != Id && x.Celular == datos.Celular))
                    throw new CampoYaExistenteFailException(datos.Celular);

            if (datos.Dni != null && datos.Dni != cliente.Dni)
                if (await _clientes.ExisteAsync(x => x.Id != Id && x.Dni == datos.Dni))
                    throw new CampoYaExistenteFailException(datos.Dni.ToString()!);

            if (!string.IsNullOrEmpty(datos.Correo) && datos.Correo != cliente.Correo)
                if (await _clientes.ExisteAsync(x => x.Id != Id && x.Correo == datos.Correo.ToLower()))
                    throw new CampoYaExistenteFailException(datos.Correo);

            datos.Adapt(cliente);
            cliente.Correonormalizado = !string.IsNullOrEmpty(datos.Correo)
                ? datos.Correo.ToUpper()
                : string.Empty;

            await _clientes.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{Id:int}")]
        [Authorize(Roles = RolesKafe.Admin)]
        public async Task<IActionResult> Delete(int Id)
        {
            if (Id <= 0) return BadRequest(new { message = "Cliente no encontrado" });

            var cliente = await _clientes.FindByIdAsync(Id);

            if (cliente == null) return BadRequest("Cliente no encontrado");

            await _clientes.Remove(cliente);

            await _clientes.SaveAsync();

            return NoContent();
            
        }
    }

}
