using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MensajesController(IHttpClientFactory _http) : ControllerBase
    {
        [HttpPost("Cumple")]
        [Authorize]
        public async Task<IActionResult> MensajesCumpleanos()
        {
            var client = _http.CreateClient("MensajesApi");

            try
            {
                var res = new HttpRequestMessage(HttpMethod.Post, "webhook/cumpleanos");
                HttpResponseMessage respuesta = await client.SendAsync(res);

                if (respuesta.IsSuccessStatusCode)
                {
                    return Ok(respuesta.Content);
                }
            }
            catch
            {
                return BadRequest("Algo salio mal");
            }

            return Ok();
        }
    }
}
