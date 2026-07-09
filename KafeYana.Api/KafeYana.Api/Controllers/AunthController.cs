using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.IServicios;
using KafeYana.Core.Entities.Entity;
using KafeYana.Domain.Request;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AunthController(IAccountService _servicio, UserManager<Usuario> _userManager) : ControllerBase
    {
        [HttpPost("Registro")]
  
        public async Task<IActionResult> Registro(RegisterRequest datos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _servicio.Register(datos);

            return Ok(new { message = "Usuario Registrado" });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest datos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _servicio.Login(datos);

            return Ok(usuario);
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            var RefreshToken = Request.Cookies["REFRESH_TOKEN"];

            if (string.IsNullOrEmpty(RefreshToken))
            {
                return BadRequest(new { Message = "Token No encontrado" });
            }

            var usuario = await _servicio.RefreshTokenAsync(RefreshToken);

            return Ok(usuario);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var email = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            await _servicio.Logout(email);
            return NoContent();
        }

        [HttpPut("bloquear/{email}")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Bloquear(string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            var roles = await _userManager.GetRolesAsync(usuario);
            if (roles.Contains(RolesKafe.Admin))
                return BadRequest(new { message = "No puedes bloquear a un administrador" });

            await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

            return Ok(new { message = "Usuario bloqueado correctamente" });
        }

        [HttpPut("desbloquear/{email}")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Desbloquear(string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            await _userManager.SetLockoutEndDateAsync(usuario, null);

            return Ok(new { message = "Usuario desbloqueado correctamente" });
        }

        [HttpDelete("{email}")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Eliminar(string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            var roles = await _userManager.GetRolesAsync(usuario);
            if (roles.Contains(RolesKafe.Admin))
                return BadRequest(new { message = "No puedes eliminar a un administrador" });

            var resultado = await _userManager.DeleteAsync(usuario);
            if (!resultado.Succeeded)
                return BadRequest(new { message = resultado.Errors.First().Description });

            return Ok(new { message = "Usuario eliminado correctamente" });
        }

        [HttpPut("info")]
        [Authorize]
        public async Task<IActionResult> CambiarDatos(DtoUsuarioDatoU datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailActual = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailActual))
                return Unauthorized(new { message = "Usuario no identificado" });

            var usuario = await _userManager.FindByEmailAsync(emailActual);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (!string.Equals(usuario.UserName, datos.usuario, StringComparison.OrdinalIgnoreCase))
            {
                var existe = await _userManager.FindByNameAsync(datos.usuario);
                if (existe != null && existe.Id != usuario.Id)
                    return BadRequest(new { message = "El nombre de usuario ya está en uso" });
            }

            usuario.Email = datos.email.ToLower();
            usuario.UserName = datos.usuario;
            usuario.NormalizedEmail = datos.email.ToUpper();
            usuario.NormalizedUserName = datos.usuario.ToUpper();
            usuario.Nombre = datos.nombre;
            usuario.Apellido = datos.apellido;
            usuario.PhoneNumber = datos.telefono;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                var error = resultado.Errors.First();
                if (error.Code == "DuplicateUserName")
                    return BadRequest(new { message = "Intente con otro usuario o email" });
                if (error.Code == "DuplicateEmail")
                    return BadRequest(new { message = "Intente con otro email" });
                return BadRequest(new { message = error.Description });
            }

            return Ok(new { message = "Datos actualizados" });

        }

        [HttpPut("new-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword(DtoCambiarPassword datos)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailActual = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailActual))
                return Unauthorized(new { message = "Usuario no identificado" });

            var usuario = await _userManager.FindByEmailAsync(emailActual);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            var resultado = await _userManager.ChangePasswordAsync(usuario, datos.PasswordActual, datos.PasswordNueva);

            if (!resultado.Succeeded)
            {
                var error = resultado.Errors.First();

                // Usamos un switch o una serie de if para traducir códigos específicos
                return error.Code switch
                {
                    "PasswordMismatch" => BadRequest(new { message = "La contraseña actual es incorrecta." }),
                    "DuplicateUserName" => BadRequest(new { message = "Intente con otro email." }),
                    "PasswordTooShort" => BadRequest(new { message = "La nueva contraseña es muy corta." }),
                    _ => BadRequest(new { message = error.Description }) // Mensaje por defecto de Identity
                };
            }

            return Ok(new { message = "Datos actualizados correctamente" });
        }
    }
}
