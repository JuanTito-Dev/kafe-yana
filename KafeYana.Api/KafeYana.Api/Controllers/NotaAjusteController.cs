using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotaAjusteController : ControllerBase
    {
        private readonly INotaAjusteSiatEnvioService _envioService;
        private readonly INotaAjusteSiatAnulacionService _anulacionService;
        private readonly INotaAjusteSiatReversionAnulacionService _reversionService;
        private readonly IUnitWork _db;

        public NotaAjusteController(
            INotaAjusteSiatEnvioService envioService,
            INotaAjusteSiatAnulacionService anulacionService,
            INotaAjusteSiatReversionAnulacionService reversionService,
            IUnitWork db)
        {
            _envioService = envioService;
            _anulacionService = anulacionService;
            _reversionService = reversionService;
            _db = db;
        }

        /// <summary>
        /// Crea, prepara y envía una nota de crédito/débito al SIAT sobre una venta Validada.
        /// La venta original NO se modifica. Devuelve el resultado del envío.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> EmitirNota(
            [FromBody] DtoCrearNotaAjuste dto,
            CancellationToken ct)
        {
            try
            {
                var resultado = await _envioService.EmitirYEnviarNotaAsync(dto.IdVenta, dto, ct);

                var mensaje = resultado.Transaccion
                    ? resultado.Enviado
                        ? "Nota de ajuste emitida y validada por el SIAT."
                        : "La nota ya estaba validada por el SIAT."
                    : "El SIAT rechazó la nota o hubo error de comunicación.";

                return Ok(new
                {
                    message = mensaje,
                    Siat = resultado
                });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Reenvía al SIAT una nota pendiente u observada.</summary>
        [HttpPost("{id:int}/reenviar")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ReenviarNota(int id, CancellationToken ct)
        {
            try
            {
                var resultado = await _envioService.ReenviarNotaAsync(id, ct);

                var mensaje = resultado.Transaccion
                    ? resultado.Enviado
                        ? "Nota reenviada y validada por el SIAT."
                        : "La nota ya estaba validada por el SIAT."
                    : "Nota reenviada al SIAT con observaciones o error de comunicación.";

                return Ok(new
                {
                    message = mensaje,
                    NotaAjusteId = id,
                    Siat = resultado
                });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Obtiene una nota con sus detalles.</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> Obtener(int id)
        {
            var nota = await _db.notasAjuste.TraerNotaAjusteConDetallesAsync(id);
            if (nota is null)
                return NotFound(new { error = $"NotaAjuste {id} no encontrada." });

            return Ok(nota);
        }

        /// <summary>Lista todas las notas asociadas a una venta (más recientes primero).</summary>
        [HttpGet("por-venta/{ventaId:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> ListarPorVenta(int ventaId)
        {
            var notas = await _db.notasAjuste.ListarPorVentaAsync(ventaId);
            var resumen = notas.Select(n => new DtoNotaAjusteResumen
            {
                Id = n.Id,
                IdVenta = n.IdVenta,
                NumeroNotaCreditoDebito = n.NumeroNotaCreditoDebito,
                Cuf = n.Cuf,
                EstadoSiat = n.EstadoSiat?.ToString(),
                CodigoRecepcion = n.CodigoRecepcion,
                CodigoMotivoAjuste = n.CodigoMotivoAjuste,
                FechaEmision = n.FechaEmision,
                MontoTotalOriginal = n.MontoTotalOriginal,
                MontoTotalDevuelto = n.MontoTotalDevuelto,
                MontoEfectivoCreditoDebito = n.MontoEfectivoCreditoDebito,
                RevertidaAnulacion = n.RevertidaAnulacion
            }).ToList();

            return Ok(new
            {
                ventaId,
                total = resumen.Count,
                notas = resumen
            });
        }

        /// <summary>
        /// Anula en el SIAT una nota de crédito/débito previamente validada (EstadoSiat = 908).
        /// Solo aplica a notas en estado Validada y que no hayan revertido su anulación.
        /// </summary>
        [HttpPost("anular/{id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> AnularNota(
            int id,
            [FromBody] DtoAnularNotaAjuste dto,
            CancellationToken ct)
        {
            try
            {
                var anulacion = await _anulacionService.AnularNotaAsync(id, dto.CodigoMotivo, ct);

                var nota = await _db.notasAjuste.FindByIdAsync(id);

                var mensaje = anulacion.Transaccion
                    ? anulacion.EstadoSiat == FacturaEstado.Anulada
                        ? "Nota de ajuste anulada correctamente en el SIAT."
                        : "La nota ya estaba anulada."
                    : "El SIAT rechazó la anulación o hubo error de comunicación.";

                return Ok(new
                {
                    message = mensaje,
                    NotaAjusteId = id,
                    NumeroNotaCreditoDebito = nota?.NumeroNotaCreditoDebito,
                    CodigoMotivo = dto.CodigoMotivo,
                    MotivoDescripcion = MotivoAnulacionSiatCatalogo
                        .ObtenerDescripcion(dto.CodigoMotivo),
                    Siat = anulacion
                });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Revierte en el SIAT la anulación errónea de una nota C/D (EstadoSiat = 950).
        /// Solo permitido una vez por nota.
        /// </summary>
        [HttpPost("revertir-anulacion/{id:int}")]
        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
        public async Task<IActionResult> RevertirAnulacionNota(int id, CancellationToken ct)
        {
            try
            {
                var reversion = await _reversionService.RevertirAnulacionNotaAsync(id, ct);

                var mensaje = reversion.Transaccion
                    ? "Anulación de la nota revertida correctamente en el SIAT. Volvió a estado Validada (908)."
                    : "El SIAT rechazó la reversión de anulación o hubo error de comunicación.";

                return Ok(new
                {
                    message = mensaje,
                    NotaAjusteId = id,
                    Siat = reversion
                });
            }
            catch (VentaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
