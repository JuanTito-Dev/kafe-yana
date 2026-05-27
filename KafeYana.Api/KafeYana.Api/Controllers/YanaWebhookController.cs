// Controllers/YanaWebhookController.cs
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; 

namespace YanaBot.Api.Controllers;

[ApiController]
[Route("api/yana")]
[Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
public class YanaWebhookController : ControllerBase
{
    private readonly YanaBotService _yanaBotService;
    private readonly ILogger<YanaWebhookController> _logger;

    public YanaWebhookController(YanaBotService yanaBotService, ILogger<YanaWebhookController> logger)
    {
        _yanaBotService = yanaBotService;
        _logger = logger;
    }

    [HttpPost("cumpleanos")]
    public async Task<IActionResult> DispararCumpleanos()
    {
        try
        {
            var resultado = await _yanaBotService.DispararCumpleanosAsync();
            return Ok(resultado); // 👈 directo, sin envolver
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al conectar con el cerebro Python (cumpleaños)");
            return StatusCode(502, new { status = "error", mensaje = "No se pudo conectar con el cerebro Python.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en campaña de cumpleaños");
            return StatusCode(500, new { status = "error", mensaje = ex.Message });
        }
    }

    [HttpPost("temporada")]
    public async Task<IActionResult> DispararTemporada()
    {
        try
        {
            var resultado = await _yanaBotService.DispararTemporadaAsync();
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al conectar con el cerebro Python (temporada)");
            return StatusCode(502, new { status = "error", mensaje = "No se pudo conectar con el cerebro Python.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en campaña de temporada");
            return StatusCode(500, new { status = "error", mensaje = ex.Message });
        }
    }

    [HttpPost("puntos")]
    public async Task<IActionResult> DispararPuntos()
    {
        try
        {
            var resultado = await _yanaBotService.DispararPuntosAsync();
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al conectar con el cerebro Python ventas");
            return StatusCode(502, new { status = "error", mensaje = "No se pudo conectar con el cerebro Python.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en campaña de ventas");
            return StatusCode(500, new { status = "error", mensaje = ex.Message });
        }
    }

    [HttpPost("permanentes")]
    public async Task<IActionResult> DispararPermanentes()
    {
        try
        {
            var resultado = await _yanaBotService.DispararPermanentesAsync();
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al conectar con el cerebro Python permanentes");
            return StatusCode(502, new { status = "error", mensaje = "No se pudo conectar con el cerebro Python.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en campaña de permanentes");
            return StatusCode(500, new { status = "error", mensaje = ex.Message });
        }
    }
}
