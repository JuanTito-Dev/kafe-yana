using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.Autentication;
using UsaAutoPartes.Application.Dtos.Authentication;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Domain.Enum.CookieNames;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthenticationRepositorio _Auth) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Registro(RequestRegister Datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _Auth.Register(Datos);

            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult<DtoUsuarioDatos>> Login(RequestLogin datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var usuario = await _Auth.Login(datos);

            return Ok(usuario);
        }

        [HttpPost("refresh")]

        public async Task<ActionResult<DtoUsuarioDatos>> RefreshToken()
        {
            var token = Request.Cookies[CookiesNames.accessreload.ToString()];

            var usuario = await _Auth.RefreshTokenAsync(token);

            return Ok(usuario);
        }
    }
}
