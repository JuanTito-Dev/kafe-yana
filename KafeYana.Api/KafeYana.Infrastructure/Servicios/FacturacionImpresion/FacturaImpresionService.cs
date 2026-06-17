using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    public class FacturaImpresionService(
        IUnitWork _db,
        IOptions<FacturaImpresoraOptions> options,
        ILogger<FacturaImpresionService> logger) : IFacturaImpresionService
    {
        private readonly FacturaImpresoraOptions _opts = options.Value;

        public async Task<ResultadoImpresionFacturaDto> ImprimirVentaAsync(
            Venta venta,
            CancellationToken ct = default)
        {
            if (!venta.Facturado)
            {
                return new ResultadoImpresionFacturaDto
                {
                    Enviado = false,
                    Ok = false,
                    ErrorMensaje = "La venta no fue emitida como factura electrónica."
                };
            }

            if (venta.EstadoSiat == FacturaEstado.Anulada)
            {
                return new ResultadoImpresionFacturaDto
                {
                    Enviado = false,
                    Ok = false,
                    ErrorMensaje = "No se puede imprimir una venta anulada."
                };
            }

            if (venta.Detalles.Count == 0)
            {
                return new ResultadoImpresionFacturaDto
                {
                    Enviado = false,
                    Ok = false,
                    ErrorMensaje = "La venta no tiene detalle para imprimir."
                };
            }

            if (string.IsNullOrWhiteSpace(venta.Cuf) || venta.Cuf.StartsWith("PENDIENTE", StringComparison.OrdinalIgnoreCase))
            {
                return new ResultadoImpresionFacturaDto
                {
                    Enviado = false,
                    Ok = false,
                    ErrorMensaje = "La venta no tiene CUF valido para generar el QR."
                };
            }

            var urlQr = FacturaQrUrlBuilder.Construir(venta);
            var builder = new FacturaTicketBuilder(_opts.AnchoCaracteres);
            var ticket = builder.Construir(venta, urlQr);

            var (ok, error) = await EnviarTcpAsync(ticket, ct);

            return new ResultadoImpresionFacturaDto
            {
                Enviado = true,
                Ok = ok,
                ErrorMensaje = error,
                UrlQr = urlQr
            };
        }

        public async Task<ResultadoImpresionFacturaDto> ImprimirPorIdAsync(
            int ventaId,
            CancellationToken ct = default)
        {
            var venta = await _db.ventas.TraerVentaConDetallesAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

            return await ImprimirVentaAsync(venta, ct);
        }

        private async Task<(bool ok, string? error)> EnviarTcpAsync(byte[] data, CancellationToken ct)
        {
            if (_opts.DevMode)
            {
                logger.LogInformation(
                    "[SIM:FACTURA]\n{Texto}\n--- fin ticket ---",
                    DecodificarTicket(data));
                return (true, null);
            }

            if (string.IsNullOrWhiteSpace(_opts.Ip))
                return (false, "FacturaImpresora.Ip no configurada.");

            string? ultimoError = null;
            for (var intento = 1; intento <= 3; intento++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var tcp = new TcpClient();
                    tcp.SendTimeout = 3000;
                    tcp.ReceiveTimeout = 3000;
                    await tcp.ConnectAsync(_opts.Ip, _opts.Port, ct);
                    await using var stream = tcp.GetStream();
                    await stream.WriteAsync(data, ct);
                    await stream.FlushAsync(ct);
                    logger.LogInformation(
                        "Factura impresa OK en {Ip}:{Port} (intento {N})",
                        _opts.Ip,
                        _opts.Port,
                        intento);
                    return (true, null);
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    logger.LogWarning(
                        "Impresion factura intento {N}/3 fallido -> {Ip}:{Port} — {Error}",
                        intento,
                        _opts.Ip,
                        _opts.Port,
                        ex.Message);
                    if (intento < 3)
                        await Task.Delay(500, ct);
                }
            }

            logger.LogError(
                "Impresion factura fallo -> {Ip}:{Port} — {Error}",
                _opts.Ip,
                _opts.Port,
                ultimoError);
            return (false, ultimoError);
        }

        private static string DecodificarTicket(byte[] data)
        {
            var sb = new StringBuilder();
            var i = 0;
            while (i < data.Length)
            {
                var b = data[i];
                if (b == 0x1B || b == 0x1D)
                {
                    i += b == 0x1B ? 3 : 4;
                    continue;
                }
                if (b == 0x0A)
                    sb.AppendLine();
                else if (b >= 0x20 && b < 0x7F)
                    sb.Append((char)b);
                i++;
            }
            return sb.ToString().Trim();
        }
    }
}
