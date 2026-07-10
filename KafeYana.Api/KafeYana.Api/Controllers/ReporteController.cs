using KafeYana.Api.Reportes;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class ReporteController(
        ReporteInventarioService _reporteInventario,
        ReporteCajaService _reporteCaja,
        ReporteProductosService _reporteProductos) : ControllerBase
    {
        /// <summary>Devuelve el resumen de inventario como PDF descargable.</summary>
        [HttpGet("inventario")]
        public async Task<IActionResult> Inventario()
        {
            var datos = await _reporteInventario.GenerarAsync();
            var documento = new ReporteInventarioPdf(datos);
            var pdfBytes = documento.GeneratePdf();
            var nombre = $"inventario_{datos.GeneradoEn:yyyyMMdd_HHmm}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }

        /// <summary>
        /// Reporte DIARIO de caja para una fecha específica (TZ La Paz).
        /// Si no se indica fecha, usa hoy. Devuelve PDF descargable.
        /// </summary>
        [HttpGet("caja-diario")]
        public async Task<IActionResult> CajaDiario([FromQuery] DateTime? fecha)
        {
            var datos = await _reporteCaja.GenerarReporteDiarioAsync(fecha ?? DateTime.UtcNow);
            var documento = new ReporteCajaDiarioPdf(datos);
            var pdfBytes = documento.GeneratePdf();
            var nombre = $"caja-diario_{datos.Fecha:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }

        /// <summary>
        /// Reporte MENSUAL de productos más vendidos / mayor rotación.
        /// Si no se pasan mes/anio, usa el mes actual en TZ La Paz.
        /// </summary>
        [HttpGet("productos-mensual")]
        public async Task<IActionResult> ProductosMensual([FromQuery] int? mes, [FromQuery] int? anio)
        {
            var datos = await _reporteProductos.GenerarReporteMensualAsync(mes, anio);
            var documento = new ReporteProductosMensualPdf(datos);
            var pdfBytes = documento.GeneratePdf();
            var nombre = $"productos-mensual_{datos.Anio}-{datos.Mes:D2}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }
    }
}
