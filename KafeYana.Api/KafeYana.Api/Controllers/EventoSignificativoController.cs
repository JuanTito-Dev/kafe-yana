using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KafeYana.Api.Controllers
{
    /// <summary>
    /// Endpoints REST para el flujo de contingencia SIAT Bolivia.
    ///
    /// Permite al cajero registrar manualmente un evento (cuando detecta que
    /// no hay internet), al frontend consultar el estado actual para mostrar
    /// la UI adecuada, y al operador ver el historial + motivos del catálogo.
    ///
    /// En el siguiente paso (detector automático) el endpoint POST /registrar
    /// también será invocado por el wrapper detector del backend cuando
    /// detecte timeout/503 contra el SIAT.
    ///
    /// Ver [[kafeyana-contingencia-siat]] — arquitectura backend autoridad.
    /// </summary>
    [ApiController]
    [Route("api/evento-significativo")]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class EventoSignificativoController : ControllerBase
    {
        private readonly IEventoSignificativoSiatService _service;
        private readonly ISiatConnectivityMonitor _monitor;
        private readonly SiatOptions _siatOpts;

        public EventoSignificativoController(
            IEventoSignificativoSiatService service,
            ISiatConnectivityMonitor monitor,
            IOptions<SiatOptions> siatOpts)
        {
            _service = service;
            _monitor = monitor;
            _siatOpts = siatOpts.Value;
        }

        /// <summary>
        /// Registra un evento significativo ante el SIN. El backend resuelve el
        /// CUIS/CUFD vigentes y arma el sobre SOAP <c>registroEventoSignificativo</c>.
        /// Persiste la respuesta (con <c>codigoRecepcionEventoSignificativo</c>) en BD.
        ///
        /// <b>Flujo unificado para los 7 motivos del SIN</b> — un solo endpoint,
        /// una sola implementación en el backend, parametrizado por
        /// <c>codigoMotivo</c>. Catálogo vigente (jun-2026):
        /// <list type="bullet">
        ///   <item>1 — CORTE DEL SERVICIO DE INTERNET</item>
        ///   <item>2 — INACCESIBILIDAD AL SERVICIO WEB DE LA ADMINISTRACIÓN TRIBUTARIA</item>
        ///   <item>3 — INGRESO A ZONAS SIN INTERNET POR DESPLIEGUE DE PUNTO DE VENTA</item>
        ///   <item>4 — VENTA EN LUGARES SIN INTERNET</item>
        ///   <item>5 — VIRUS INFORMÁTICO O FALLA DE SOFTWARE</item>
        ///   <item>6 — CAMBIO DE INFRAESTRUCTURA DE SISTEMA O FALLA DE HARDWARE</item>
        ///   <item>7 — CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Body mínimo (descripcion se auto-rellena desde el catálogo):
        /// <code>
        /// {
        ///   "codigoMotivo": 4
        /// }
        /// </code>
        /// Body con descripción custom (ej: operador agrega contexto extra):
        /// <code>
        /// {
        ///   "codigoMotivo": 5,
        ///   "descripcion": "VIRUS INFORMÁTICO — nodo 3 infectado, software desactualizado"
        /// }
        /// </code>
        /// <c>fechaHoraInicioEvento</c> y <c>fechaHoraFinEvento</c> son opcionales:
        /// si se omiten, el backend asume inicio=ahora y fin=inicio+2min.
        /// <c>codigoSucursal</c> y <c>codigoPuntoVenta</c> también son opcionales
        /// (default = appsettings).
        /// </remarks>
        [HttpPost("registrar")]
        [ProducesResponseType(typeof(ResultadoRegistroEventoSignificativoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Registrar(
            [FromBody] DtoRegistrarEventoSignificativo dto,
            CancellationToken ct)
        {
            try
            {
                var suc = dto.CodigoSucursal ?? _siatOpts.CodigoSucursal;
                var pv  = dto.CodigoPuntoVenta ?? _siatOpts.CodigoPuntoVenta;
                var resultado = await _service.RegistrarYActivarAsync(
                    dto.CodigoMotivo,
                    "Manual",
                    suc,
                    pv,
                    dto.Descripcion ?? string.Empty,
                    ct);
                _monitor.NotificarContingenciaExterna(suc, pv, resultado.EventoId);
                return Ok(resultado);
            }
            catch (VentaException ex) when (ex.Message.Contains("Ya existe una contingencia activa"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "No se pudo comunicar con el SIAT.",
                    detalle = ex.Message
                });
            }
        }

        /// <summary>
        /// Devuelve el estado actual de contingencia para un PV. Si <c>contingenciaActiva=false</c>,
        /// el resto de campos vienen vacíos.
        /// Lo consume el frontend para decidir si mostrar el banner "Modo contingencia".
        /// </summary>
        [HttpGet("estado")]
        [ProducesResponseType(typeof(EstadoContingenciaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerEstado(
            [FromQuery] int? sucursal,
            [FromQuery] int? puntoVenta,
            CancellationToken ct)
        {
            // Default a appsettings si el frontend no lo manda (compatibilidad con header legacy).
            int suc, pv;
            if (sucursal.HasValue && puntoVenta.HasValue)
            {
                suc = sucursal.Value;
                pv = puntoVenta.Value;
            }
            else
            {
                var siatSection = HttpContext.RequestServices
                    .GetService(typeof(Microsoft.Extensions.Options.IOptions<
                        KafeYana.Infrastructure.Configuration.SiatOptions>))
                    as Microsoft.Extensions.Options.IOptions<
                        KafeYana.Infrastructure.Configuration.SiatOptions>;

                suc = siatSection?.Value.CodigoSucursal ?? 0;
                pv = siatSection?.Value.CodigoPuntoVenta ?? 0;
            }

            var estado = await _service.ObtenerEstadoContingenciaAsync(suc, pv, ct);
            return Ok(estado);
        }

        /// <summary>
        /// Lista el historial de eventos significativos para un PV, ordenado
        /// por fecha de registro descendente. Límite por defecto 50, máximo 200.
        /// </summary>
        [HttpGet("historial")]
        [ProducesResponseType(typeof(List<EventoSignificativoHistorialDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarHistorial(
            [FromQuery] int? sucursal,
            [FromQuery] int? puntoVenta,
            [FromQuery] int limite = 50,
            CancellationToken ct = default)
        {
            int suc = sucursal ?? 0;
            int pv = puntoVenta ?? 0;
            var historial = await _service.ListarHistorialAsync(suc, pv, limite, ct);
            return Ok(historial);
        }

        /// <summary>
        /// Cierra una contingencia activa (al recuperar la conexión con el SIAT).
        /// Marca el evento como Cerrado en BD. Las próximas facturas se emitirán
        /// en línea sin metadata de contingencia.
        /// </summary>
        [HttpPost("cerrar/{eventoSignificativoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Cerrar(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            await _service.CerrarContingenciaAsync(eventoSignificativoId, ct);
            return NoContent();
        }

        /// <summary>
        /// Devuelve los motivos del catálogo SIAT (los 7 eventos significativos
        /// reconocidos por el SIN). Lo consume el frontend para llenar el
        /// dropdown del formulario de registro.
        /// </summary>
        [HttpGet("motivos")]
        [ProducesResponseType(typeof(List<MotivoCatalogoDto>), StatusCodes.Status200OK)]
        public IActionResult ListarMotivos()
        {
            // Lista oficial vigente (jun-2026, devuelta por el SIN).
            // Idéntica a CatEventosSignificativos en BD pero la hardcodeamos
            // acá para que el endpoint funcione incluso si el sync diario
            // aún no corrió (el primer cobro del día en una PC recién prendida).
            var motivos = new List<MotivoCatalogoDto>
            {
                new() { Codigo = 1, Descripcion = "CORTE DEL SERVICIO DE INTERNET" },
                new() { Codigo = 2, Descripcion = "INACCESIBILIDAD AL SERVICIO WEB DE LA ADMINISTRACIÓN TRIBUTARIA" },
                new() { Codigo = 3, Descripcion = "INGRESO A ZONAS SIN INTERNET POR DESPLIEGUE DE PUNTO DE VENTA" },
                new() { Codigo = 4, Descripcion = "VENTA EN LUGARES SIN INTERNET" },
                new() { Codigo = 5, Descripcion = "VIRUS INFORMÁTICO O FALLA DE SOFTWARE" },
                new() { Codigo = 6, Descripcion = "CAMBIO DE INFRAESTRUCTURA DE SISTEMA O FALLA DE HARDWARE" },
                new() { Codigo = 7, Descripcion = "CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA" }
            };
            return Ok(motivos);
        }
    }

    /// <summary>DTO liviano para motivos del catálogo.</summary>
    public class MotivoCatalogoDto
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}