using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturacionController : ControllerBase
    {
        private readonly ICuisService _cuis;
        private readonly ICufdService _cufd;
        private readonly IVerificaNitService _verificaNit;
        private readonly SiatOptions _opts;

        public FacturacionController(
            ICuisService cuisService,
            ICufdService cufdService,
            IVerificaNitService verificaNitService,
            IOptions<SiatOptions> opts)
        {
            _cuis = cuisService;
            _cufd = cufdService;
            _verificaNit = verificaNitService;
            _opts = opts.Value;
        }

        /// <summary>
        /// Obtiene el CUIS vigente (o solicita uno nuevo si venció).
        /// Úsalo para verificar que la conexión con el SIAT funciona.
        /// </summary>
        [HttpGet("cuis")]
        public async Task<IActionResult> ObtenerCuis(
            [FromQuery] int sucursal = 0,
            [FromQuery] int puntoVenta = 0,
            CancellationToken ct = default)
        {
            try
            {
                var cuis = await _cuis.ObtenerCuisVigenteAsync(sucursal, puntoVenta, ct);
                return Ok(new
                {
                    id = cuis.Id,
                    codigoCuis = cuis.Codigo,
                    fechaVigencia = cuis.FechaVigencia,
                    codigoSucursal = cuis.CodigoSucursal,
                    codigoPuntoVenta = cuis.CodigoPuntoVenta,
                    fechaRegistro = cuis.FechaRegistro,
                    esVigente = cuis.EsVigente()
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el CUFD del día (o solicita uno nuevo si venció).
        /// </summary>
        [HttpGet("cufd")]
        public async Task<IActionResult> ObtenerCufd(
            [FromQuery] int sucursal = 0,
            [FromQuery] int puntoVenta = 0,
            CancellationToken ct = default)
        {
            try
            {
                var cufd = await _cufd.ObtenerCufdVigenteAsync(sucursal, puntoVenta, ct);
                return Ok(new
                {
                    id = cufd.Id,
                    codigoCufd = cufd.Codigo,
                    codigoControl = cufd.CodigoControl,
                    direccion = cufd.Direccion,
                    fechaVigencia = cufd.FechaVigencia,
                    codigoSucursal = cufd.CodigoSucursal,
                    codigoPuntoVenta = cufd.CodigoPuntoVenta,
                    fechaRegistro = cufd.FechaRegistro,
                    esVigente = cufd.EsVigente()
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Verifica si un NIT es válido ante el SIN.
        /// Llámalo antes de emitir una factura a un cliente nuevo.
        /// </summary>
        [HttpGet("verificar-nit/{nit}")]
        public async Task<IActionResult> VerificarNit(
            long nit,
            [FromQuery] int sucursal = 0,
            [FromQuery] int puntoVenta = 0,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _verificaNit.VerificarNitAsync(
                    nit, sucursal, puntoVenta, ct);

                return Ok(new
                {
                    nit = result.Nit,
                    valido = result.Valido,
                    transaccion = result.Transaccion,
                    mensajes = result.Mensajes.Select(m => new
                    {
                        codigo = m.Codigo,
                        descripcion = m.Descripcion
                    })
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
