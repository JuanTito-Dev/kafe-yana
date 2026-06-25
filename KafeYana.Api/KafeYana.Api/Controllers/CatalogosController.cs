using KafeYana.Infrastructure.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    /// <summary>
    /// Endpoints de diagnóstico para la sincronización de catálogos del SIAT.
    ///
    /// El panel de certificación del SIAT consulta estos endpoints
    /// durante las "Pruebas Correctas" (Casos 1 y 2) para verificar
    /// que el sistema está listo y mantener los datos actualizados.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogosController : ControllerBase
    {
        private readonly SincronizadorCatActividades _sincronizador;

        public CatalogosController(SincronizadorCatActividades sincronizador)
        {
            _sincronizador = sincronizador;
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-actividades
        ///
        /// Ejecuta la sincronización del catálogo de actividades contra
        /// el SIAT de forma síncrona y devuelve { transaccion: true }
        /// cuando completa. La BD se reemplaza en una transacción atómica.
        /// </summary>
        [HttpPost("sincronizar-actividades")]
        public async Task<IActionResult> SincronizarActividades(CancellationToken ct)
        {
            try
            {
                var cantidad = await _sincronizador.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad
                });
            }
            catch (InvalidOperationException ex)
            {
                // El SIAT rechazó la operación (transaccion=false o SOAP Fault).
                // Devolvemos 502 Bad Gateway para que el panel del SIAT sepa
                // que la dependencia externa falló.
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }
    }
}