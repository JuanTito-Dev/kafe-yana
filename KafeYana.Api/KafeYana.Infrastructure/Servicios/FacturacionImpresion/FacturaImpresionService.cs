using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Text;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    /// <summary>
    /// Construye el ticket ESC/POS de la factura fiscal (con QR SIAT) y lo
    /// envía por TCP a uno o varios destinos configurados en la sección
    /// unificada `Impresoras.Destinos` del appsettings.
    /// </summary>
    public class FacturaImpresionService(
        IUnitWork _db,
        IOptions<ImpresoraOptions> options,
        ILogger<FacturaImpresionService> logger) : IFacturaImpresionService
    {
        private readonly ImpresoraOptions _opts = options.Value;

        public async Task<ResultadoImpresionFacturaDto> ImprimirPorIdAsync(
            int ventaId,
            IReadOnlyList<string> destinos,
            int? anchoCaracteres,
            CancellationToken ct = default)
        {
            var venta = await _db.ventas.TraerVentaConDetallesAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

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

            if (destinos is null || destinos.Count == 0)
            {
                return new ResultadoImpresionFacturaDto
                {
                    Enviado = false,
                    Ok = false,
                    ErrorMensaje = "Debe seleccionar al menos una impresora de destino."
                };
            }

            var ancho = anchoCaracteres ?? _opts.AnchoCaracteres;
            var urlQr = FacturaQrUrlBuilder.Construir(venta);
            var builder = new FacturaTicketBuilder(ancho);
            var ticket = builder.Construir(venta, urlQr);

            var erroresPorDestino = new List<string>();
            var aciertos = 0;

            foreach (var destino in destinos)
            {
                if (!_opts.Destinos.TryGetValue(destino, out var cfg))
                {
                    erroresPorDestino.Add($"[{destino}] no existe en Impresoras.Destinos");
                    logger.LogWarning("Destino '{Destino}' no configurado en Impresoras.Destinos", destino);
                    continue;
                }

                var (ok, error) = await EnviarTcpAsync(ticket, destino, cfg.Ip, cfg.Port, ct);
                if (ok) aciertos++;
                else if (!string.IsNullOrWhiteSpace(error))
                    erroresPorDestino.Add($"[{destino}] {error}");
            }

            return new ResultadoImpresionFacturaDto
            {
                Enviado = true,
                Ok = aciertos > 0,
                ErrorMensaje = erroresPorDestino.Count == 0
                    ? null
                    : string.Join(" | ", erroresPorDestino),
                UrlQr = urlQr
            };
        }

        private async Task<(bool ok, string? error)> EnviarTcpAsync(
            byte[] data, string destino, string ip, int port, CancellationToken ct)
        {
            if (_opts.DevMode)
            {
                logger.LogInformation(
                    "[SIM:FACTURA -> {Destino}]\n{Texto}\n--- fin ticket ---",
                    destino,
                    DecodificarTicket(data));
                return (true, null);
            }

            if (string.IsNullOrWhiteSpace(ip))
                return (false, $"Impresoras.Destinos.{destino}.Ip no configurada.");

            string? ultimoError = null;
            for (var intento = 1; intento <= 3; intento++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var tcp = new TcpClient();
                    tcp.SendTimeout = 3000;
                    tcp.ReceiveTimeout = 3000;
                    await tcp.ConnectAsync(ip, port, ct);
                    await using var stream = tcp.GetStream();
                    await stream.WriteAsync(data, ct);
                    await stream.FlushAsync(ct);
                    logger.LogInformation(
                        "Factura impresa OK en {Destino} {Ip}:{Port} (intento {N})",
                        destino, ip, port, intento);
                    return (true, null);
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    logger.LogWarning(
                        "Impresion factura destino {Destino} intento {N}/3 fallido -> {Ip}:{Port} — {Error}",
                        destino, intento, ip, port, ex.Message);
                    if (intento < 3)
                        await Task.Delay(500, ct);
                }
            }

            logger.LogError(
                "Impresion factura destino {Destino} fallo -> {Ip}:{Port} — {Error}",
                destino, ip, port, ultimoError);
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