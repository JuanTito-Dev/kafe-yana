using KafeYana.Application.IServicios;
using KafeYana.Domain.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

            await _servicio.Login(datos);
            return Ok(new { message = "Usuario Encontrado" });
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            var RefreshToken = Request.Cookies["REFRESH_TOKEN"];

            if (RefreshToken == null)
            {
                return BadRequest(new {Message = "Token No encontrado"});
            }

            await _servicio.RefreshTokenAsync(RefreshToken);

            return Ok(new { message = "Token revocado"});
        }
    }
}
