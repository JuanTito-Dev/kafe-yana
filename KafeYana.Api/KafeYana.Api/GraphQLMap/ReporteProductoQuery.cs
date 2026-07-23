using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Application.Dtos.ReporteDtos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;

namespace KafeYana.Api.GraphQLMap
{
    /// <summary>
    /// Reporte mensual de productos más vendidos / con mayor rotación.
    /// </summary>
    [ExtendObjectType("Query")]
    public class ReporteProductoQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero })]
        public Task<DtoReporteMensualProductos> ReporteProductosMensual(
            [Service] ReporteProductosService _service,
            int? mes = null,
            int? anio = null,
            CancellationToken ct = default)
        {
            return _service.GenerarReporteMensualAsync(mes, anio, ct);
        }
    }
}
