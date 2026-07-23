using System;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Obtiene la fecha/hora oficial del SIN para usarla en facturas/notas.
    /// Si el SIAT está caído, lanza VentaException para bloquear el cobro.
    /// </summary>
    public class FechaHoraSiatService : IFechaHoraSiatService
    {
        private readonly SiatHttpClient _siat;
        private readonly ICuisService _cuisService;
        private readonly ILogger<FechaHoraSiatService> _logger;

        public FechaHoraSiatService(
            SiatHttpClient siat,
            ICuisService cuisService,
            ILogger<FechaHoraSiatService> logger)
        {
            _siat = siat;
            _cuisService = cuisService;
            _logger = logger;
        }

        public async Task<DateTime> ObtenerFechaHoraOficialAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            // 1) Necesitamos un CUIS vigente
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                codigoSucursal, codigoPuntoVenta, ct);

            // 2) Llamar al SOAP de sincronización
            var respuesta = await _siat.SincronizarFechaHoraAsync(
                cuis.Codigo, codigoSucursal, codigoPuntoVenta, ct);

            if (!respuesta.Transaccion || !respuesta.FechaHora.HasValue)
            {
                _logger.LogError(
                    "SIAT rechazó sincronizarFechaHora (transaccion={T}, fechaHora={F}) para PV ({S},{P})",
                    respuesta.Transaccion, respuesta.FechaHora,
                    codigoSucursal, codigoPuntoVenta);
                throw new VentaException(
                    "El SIAT no devolvió la fecha/hora oficial. "
                    + "No se puede facturar hasta que el SIAT responda.");
            }

            return respuesta.FechaHora.Value;
        }
    }
}