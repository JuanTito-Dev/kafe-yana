using KafeYana.Application.IServicios;
using KafeYana.Domain.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AunthController(IAccountService _servicio) : ControllerBase
    {
        [HttpPost("Registro")]
        public async Task<IActionResult> Registro(RegisterRequest datos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _servicio.Register(datos);

            return Ok(new { mesage = "Usuario Registrado" });
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
                return BadRequest(new {Message = "Token No encontrado"});
            }

            var usuario = await _servicio.RefreshTokenAsync(RefreshToken);

            return Ok(usuario);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["REFRESH_TOKEN"];
            await _servicio.Logout(refreshToken);
            return NoContent();
        }
    }
}
