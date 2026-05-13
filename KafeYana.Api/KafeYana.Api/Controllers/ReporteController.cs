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
    public class ReporteController(ReporteInventarioService _reporte) : ControllerBase
    {
        /// <summary>Devuelve el resumen de inventario como PDF descargable.</summary>
        [HttpGet("inventario")]
        public async Task<IActionResult> Inventario()
        {
            var datos = await _reporte.GenerarAsync();
            var documento = new ReporteInventarioPdf(datos);
            var pdfBytes = documento.GeneratePdf();
            var nombre = $"inventario_{datos.GeneradoEn:yyyyMMdd_HHmm}.pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }
    }
}
