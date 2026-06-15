using KafeYana.Application.Dtos.ClienteDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion;
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
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Crear(DtoClienteCU datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var cliente = datos.Adapt<Cliente>();

            cliente.Correonormalizado = (datos.Correo is not null && datos.Correo != string.Empty)
                ? datos.Correo.ToUpper()
                : string.Empty;

            await _clientes.Crear(cliente);
            await _clientes.SaveAsync();

            cliente.AsignarCodigoFacturacion(ClienteCodigoService.Generar(cliente.Nombre, cliente.Id));
            await _clientes.SaveAsync();

            return Created("", new { message = "Cliente creado", Id = cliente.Id, Codigo = cliente.Codigo });
        }

        [HttpPut("{Id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Editar(int Id, DtoClienteCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cliente = await _clientes.FindByIdAsync(Id);
            if (cliente == null) return NotFound(new { message = "Cliente no encontrado" });

            datos.Adapt(cliente);
            cliente.Correonormalizado = !string.IsNullOrEmpty(datos.Correo)
                ? datos.Correo.ToUpper()
                : string.Empty;

            await _clientes.SaveAsync();
            return Ok(new {message = "Cliente actualizado" });
        }

        [HttpDelete("{Id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Delete(int Id)
        {

            var cliente = await _clientes.FindByIdAsync(Id);

            if (cliente == null) return NotFound(new { message = "Cliente no encontrado" });

            await _clientes.Remove(cliente);

            await _clientes.SaveAsync();

            return Ok(new { message = "Cliente eliminado"});
            
        }
    }

}
