// Controllers/YanaWebhookController.cs
using KafeYana.Infrastructure.Servicios;
using Microsoft.AspNetCore.Mvc; 

namespace YanaBot.Api.Controllers;

[ApiController]
[Route("api/yana")]
public class YanaWebhookController : ControllerBase
{
    private readonly YanaBotService _yanaBotService;
    private readonly ILogger<YanaWebhookController> _logger;

    public YanaWebhookController(YanaBotService yanaBotService, ILogger<YanaWebhookController> logger)
    {
        _yanaBotService = yanaBotService;
        _logger = logger;
    }

    /// <summary>
    /// Dispara la campaña de cumpleaños: envía tarjetas a clientes que cumplen hoy.
    /// </summary>
    [HttpPost("cumpleanos")]
    public async Task<IActionResult> DispararCumpleanos()
    {
        try
        {
            var resultado = await _yanaBotService.DispararCumpleanosAsync();
            return Ok(new { status = "success", mensaje = resultado });
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

    /// <summary>
    /// Dispara la campaña de temporada: envía descuentos activos a todos los clientes.
    /// </summary>
    [HttpPost("temporada")]
    public async Task<IActionResult> DispararTemporada()
    {
        try
        {
            var resultado = await _yanaBotService.DispararTemporadaAsync();
            return Ok(new { status = "success", mensaje = resultado });
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
}
