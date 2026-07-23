using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Application.Dtos.ReporteDtos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios;

namespace KafeYana.Api.GraphQLMap
{
    /// <summary>
    /// Reporte diario de cajas (sesiones abiertas/cerradas en el día). Vinculado
    /// desde el KPICard "Cajas Abiertas" del Dashboard.
    /// </summary>
    [ExtendObjectType("Query")]
    public class ReporteCajaQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero })]
        public Task<DtoReporteDiarioCaja> ReporteCajaDiario(
            [Service] ReporteCajaService _service,
            DateTime? fecha = null,
            CancellationToken ct = default)
        {
            var fechaFinal = fecha ?? DateTime.UtcNow;
            return _service.GenerarReporteDiarioAsync(fechaFinal, ct);
        }
    }
}
