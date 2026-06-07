using KafeYana.Application.IRepositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodigoSiatController(ICodigoSiatRepositorio _codigos) : ControllerBase
    {
        /// <summary>
        /// Busca códigos SIAT por producto, actividad, descripciones o término general.
        /// </summary>
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? codigoProducto,
            [FromQuery] string? codigoActividad,
            [FromQuery] string? descripcionProducto,
            [FromQuery] string? descripcionActividad,
            [FromQuery] string? q,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamano = 50,
            CancellationToken ct = default)
        {
            if (pagina < 1) pagina = 1;
            if (tamano < 1) tamano = 50;
            if (tamano > 500) tamano = 500;

            var query = _codigos.Buscar(
                codigoProducto,
                codigoActividad,
                descripcionProducto,
                descripcionActividad,
                q);

            var total = await query.CountAsync(ct);

            var items = await query
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(x => new
                {
                    x.Id,
                    x.CodigoProducto,
                    x.DescripcionProducto,
                    x.CodigoActividad,
                    x.DescripcionActividad
                })
                .ToListAsync(ct);

            return Ok(new
            {
                total,
                pagina,
                tamano,
                items
            });
        }
    }
}
