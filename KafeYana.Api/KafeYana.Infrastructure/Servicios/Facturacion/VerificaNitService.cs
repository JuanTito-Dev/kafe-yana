using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class VerificaNitService : IVerificaNitService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ILogger<VerificaNitService> _logger;

        public VerificaNitService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ILogger<VerificaNitService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _logger = logger;
        }

        public async Task<VerificaNitResult> VerificarNitAsync(
            long nit,
            int codigoSucursal,
            int codigoPuntoVenta = 0,
            CancellationToken ct = default)
        {
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                codigoSucursal, codigoPuntoVenta, ct);

            var resp = await _siat.VerificarNitAsync(
                nit, cuis.Codigo, codigoSucursal, ct);

            var mensajes = resp.Mensajes
                .Select(m => new MensajeSiat
                {
                    Codigo = m.Codigo,
                    Descripcion = m.Descripcion
                })
                .ToList();

            _logger.LogInformation(
                "VerificaNIT {Nit} → transaccion:{T} | {Mensajes}",
                nit,
                resp.Transaccion,
                string.Join(" | ", mensajes.Select(m => $"[{m.Codigo}] {m.Descripcion}")));

            return new VerificaNitResult
            {
                Nit = nit,
                Valido = resp.Transaccion,
                Transaccion = resp.Transaccion,
                Mensajes = mensajes
            };
        }
    }
}
