using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace KafeYana.Api.Controllers
{
    /// <summary>Body opcional del POST /imprimir/{ventaId}.</summary>
    public class DtoImprimirFactura
    {
        /// <summary>Lista de nombres de impresora (Impresoras.Destinos). Default ["principal"].</summary>
        public List<string> Destinos { get; set; } = new();
        /// <summary>Ancho del ticket en caracteres. Default = appsettings Impresoras.AnchoCaracteres.</summary>
        public int? AnchoCaracteres { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class FacturacionController : ControllerBase
    {
        private readonly ICuisService _cuis;
        private readonly ICufdService _cufd;
        private readonly IVerificaNitService _verificaNit;
        private readonly IFacturaSiatEnvioService _facturaSiatEnvio;
        private readonly IFacturaSiatAnulacionService _facturaSiatAnulacion;
        private readonly IFacturaSiatReversionAnulacionService _facturaSiatReversionAnulacion;
        private readonly IFacturaImpresionService _facturaImpresion;
        private readonly ICatLeyendaResolver _catLeyendaResolver;
        private readonly DatosEmpresaOptions _empresaOpts;

        public FacturacionController(
            ICuisService cuisService,
            ICufdService cufdService,
            IVerificaNitService verificaNitService,
            IFacturaSiatEnvioService facturaSiatEnvio,
            IFacturaSiatAnulacionService facturaSiatAnulacion,
            IFacturaSiatReversionAnulacionService facturaSiatReversionAnulacion,
            IFacturaImpresionService facturaImpresion,
            ICatLeyendaResolver catLeyendaResolver,
            IOptions<DatosEmpresaOptions> empresaOpts)
        {
            _cuis = cuisService;
            _cufd = cufdService;
            _verificaNit = verificaNitService;
            _facturaSiatEnvio = facturaSiatEnvio;
            _facturaSiatAnulacion = facturaSiatAnulacion;
            _facturaSiatReversionAnulacion = facturaSiatReversionAnulacion;
            _facturaImpresion = facturaImpresion;
            _catLeyendaResolver = catLeyendaResolver;
            _empresaOpts = empresaOpts.Value;
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
                var cufd = await _cufd.ObtenerCufdVigenteAsync(sucursal, puntoVenta, DateTime.UtcNow, ct);
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
        /// Devuelve una leyenda aleatoria del catálogo sincronizado
        /// (`CatLeyendas`) para el CAEB del emisor.
        /// La usa el frontend para mostrar la leyenda obligatoria en la
        /// preview de impresión de la factura (la impresión real la toma
        /// el backend de `Venta.Leyenda` al persistir).
        /// </summary>
        [HttpGet("leyenda-aleatoria")]
        public async Task<IActionResult> ObtenerLeyendaAleatoria(CancellationToken ct = default)
        {
            try
            {
                var leyenda = await _catLeyendaResolver.ObtenerAleatoriaAsync("5610200", ct);
                return Ok(new { leyenda });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Imprime la representación gráfica de una factura ya guardada (80mm + QR SIAT).
        /// Acepta body opcional con `Destinos` (lista de impresoras de la sección
        /// `Impresoras.Destinos` del appsettings: principal/cocina/barra) y
        /// `AnchoCaracteres` (override del ancho de ticket; default 48 = 80mm@FontA).
        /// </summary>
        [HttpPost("imprimir/{ventaId:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ImprimirFactura(
            int ventaId,
            [FromBody] DtoImprimirFactura? body,
            CancellationToken ct = default)
        {
            var destinos = body?.Destinos ?? new List<string> { "principal" };
            var ancho = body?.AnchoCaracteres;

            var impresion = await _facturaImpresion.ImprimirPorIdAsync(
                ventaId, destinos, ancho, ct);

            return Ok(new
            {
                message = impresion.Ok
                    ? "Factura enviada a la impresora."
                    : "No se pudo imprimir la factura.",
                VentaId = ventaId,
                ImpresionFactura = impresion
            });
        }

        /// <summary>
        /// Anula en el SIAT una factura previamente validada (EstadoSiat = 908).
        /// Solo aplica a ventas emitidas como factura electrónica (Facturado = true).
        /// </summary>
        [HttpPost("anular/{ventaId:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> AnularFactura(
            int ventaId,
            [FromBody] DtoAnularFactura dto,
            CancellationToken ct)
        {
            var anulacion = await _facturaSiatAnulacion.AnularVentaAsync(ventaId, dto.CodigoMotivo, ct);

            var mensaje = anulacion.Transaccion
                ? anulacion.EstadoSiat == FacturaEstado.Anulada
                    ? "Factura anulada correctamente en el SIAT."
                    : "La factura ya estaba anulada."
                : "El SIAT rechazó la anulación o hubo error de comunicación.";

            return Ok(new
            {
                message = mensaje,
                VentaId = ventaId,
                CodigoMotivo = dto.CodigoMotivo,
                MotivoDescripcion = MotivoAnulacionSiatCatalogo.ObtenerDescripcion(dto.CodigoMotivo),
                Siat = anulacion
            });
        }

        /// <summary>
        /// Revierte en el SIAT una anulación errónea (EstadoSiat = 950). Solo permitido una vez por factura.
        /// </summary>
        [HttpPost("revertir-anulacion/{ventaId:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> RevertirAnulacionFactura(int ventaId, CancellationToken ct)
        {
            var reversion = await _facturaSiatReversionAnulacion.RevertirAnulacionVentaAsync(ventaId, ct);

            var mensaje = reversion.Transaccion
                ? "Anulación revertida correctamente en el SIAT. La factura volvió a estado Validada (908)."
                : "El SIAT rechazó la reversión de anulación o hubo error de comunicación.";

            return Ok(new
            {
                message = mensaje,
                VentaId = ventaId,
                Siat = reversion
            });
        }

        /// <summary>
        /// Envía o reenvía al SIAT una venta pendiente.
        /// Si Facturado = false, genera número, CUF/CUFD, XML y hash antes de enviar.
        /// Si ya estaba facturada, reutiliza XmlBase64 y CodigoHash guardados.
        /// </summary>
        [HttpPost("reenviar/{ventaId:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ReenviarFactura(
            int ventaId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DtoDatosFiscalesReenvio? datosFiscales,
            CancellationToken ct)
        {
            var envioSiat = await _facturaSiatEnvio.ReenviarFacturaAsync(ventaId, datosFiscales, ct);

            var mensaje = envioSiat.Transaccion
                ? envioSiat.Enviado
                    ? "Factura reenviada y validada por el SIAT."
                    : "La factura ya estaba validada por el SIAT."
                : "Factura reenviada al SIAT con observaciones o error de comunicación.";

            return Ok(new
            {
                message = mensaje,
                VentaId = ventaId,
                Siat = envioSiat
            });
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
