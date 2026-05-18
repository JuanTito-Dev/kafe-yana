using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrController(
        IUnitWork db,
        IQrImagenService qrImagen,
        IProductoImagenService eliminadorUrls) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Obtener()
        {
            var registro = await db.configuracionQr.ObtenerUnicaAsync();
            var url = registro?.Url ?? string.Empty;
            return Ok(new { Url = url });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Crear(IFormFile Imagen)
        {
            if (Imagen is null)
                return BadRequest(new { message = "La imagen es requerida." });

            if (await db.configuracionQr.ExisteAlgunaAsync())
                return BadRequest(new { message = "Ya existe un código QR configurado. Use actualizar para reemplazarlo." });

            var url = await qrImagen.SubirAsync(Imagen);
            await db.configuracionQr.Crear(new ConfiguracionQr { Url = url });
            await db.SaveUnitWork();

            return Ok(new { Url = url });
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Actualizar(IFormFile Imagen)
        {
            if (Imagen is null)
                return BadRequest(new { message = "La imagen es requerida." });

            var registro = await db.configuracionQr.ObtenerUnicaAsync();
            if (registro is null)
                return BadRequest(new { message = "No hay código QR configurado. Cree uno primero." });

            await eliminadorUrls.EliminarSiExisteAsync(registro.Url);

            registro.Url = await qrImagen.SubirAsync(Imagen);
            await db.SaveUnitWork();

            return Ok(new { Url = registro.Url });
        }

        [HttpDelete("eliminar")]
        [Authorize(Roles = $"{RolesKafe.Admin}")]
        public async Task<IActionResult> Eliminar()
        {
            var registro = await db.configuracionQr.ObtenerUnicaAsync();
            if (registro is null)
                return BadRequest(new { message = "No hay código QR para eliminar." });

            await eliminadorUrls.EliminarSiExisteAsync(registro.Url);
            await db.configuracionQr.Remove(registro);
            await db.SaveUnitWork();

            return Ok(new { message = "Código QR eliminado correctamente." });
        }
    }
}
