using KafeYana.Application.Dtos.ImpresoraDtos;
using KafeYana.Application.IServicios;
using KafeYana.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace KafeYana.Infrastructure.Servicios
{
    public class ImpresoraService(IOptions<ImpresoraOptions> options, ILogger<ImpresoraService> logger) : IImpresoraService
    {
        private readonly ImpresoraOptions _opts = options.Value;

        private static readonly byte[] INIT         = [0x1B, 0x40];
        private static readonly byte[] BOLD_ON      = [0x1B, 0x45, 0x01];
        private static readonly byte[] BOLD_OFF     = [0x1B, 0x45, 0x00];
        private static readonly byte[] ALIGN_CENTER = [0x1B, 0x61, 0x01];
        private static readonly byte[] ALIGN_LEFT   = [0x1B, 0x61, 0x00];
        private static readonly byte[] BIG          = [0x1D, 0x21, 0x11];
        private static readonly byte[] NORMAL       = [0x1D, 0x21, 0x00];
        private static readonly byte[] CUT          = [0x1D, 0x56, 0x41, 0x10];
        private static readonly byte[] LF           = [0x0A];

        private static readonly Encoding Enc = Encoding.GetEncoding("iso-8859-1");

        private static readonly Dictionary<string, string> MetodoLabel = new()
        {
            ["cash"]     = "EFECTIVO",
            ["card"]     = "TARJETA",
            ["transfer"] = "TRANSFERENCIA / QR",
        };

        /// <summary>
        /// Un SemaphoreSlim(1,1) por destino. Serializa TODAS las peticiones a la misma
        /// impresora: si llegan 5 comandas simultáneas a cocina, se procesan una tras
        /// otra en vez de competir por el buffer TCP del puerto 9100 (que se desborda
        /// y pierde bytes). Destinos distintos NO se bloquean entre sí.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _destinoLocks = new(StringComparer.OrdinalIgnoreCase);

        private SemaphoreSlim GetLock(string destino) =>
            _destinoLocks.GetOrAdd(destino.ToLower(), _ => new SemaphoreSlim(1, 1));

        public async Task<List<ResultadoImpresoraDto>> EnviarPedidoAsync(PedidoImprimirDto dto)
        {
            var porDestino = new Dictionary<string, List<ItemImprimirDto>>();

            if (dto.Destinos.Contains("principal", StringComparer.OrdinalIgnoreCase))
                porDestino["principal"] = dto.Items;

            foreach (var item in dto.Items)
            {
                var ub = item.Ubicacion?.ToLower() ?? "";
                if ((ub == "cocina" || ub == "barra") &&
                    dto.Destinos.Contains(ub, StringComparer.OrdinalIgnoreCase))
                {
                    if (!porDestino.TryGetValue(ub, out var lista))
                        porDestino[ub] = lista = [];
                    lista.Add(item);
                }
            }

            if (porDestino.Count == 0)
                return [];

            var resultados = new List<ResultadoImpresoraDto>();
            foreach (var (destino, items) in porDestino)
            {
                byte[] ticket = destino == "principal"
                    ? BuildTicketPrincipal(dto.Mesa, dto.Ronda, items)
                    : BuildTicket(destino, dto.Mesa, dto.Ronda, items);

                var (ok, error) = await EnviarAsync(destino, ticket);
                resultados.Add(new ResultadoImpresoraDto { Destino = destino, Ok = ok, Error = error });
            }
            return resultados;
        }

        public async Task<List<ResultadoImpresoraDto>> EnviarCuentaAsync(CuentaImprimirDto dto)
        {
            var porDestino = new Dictionary<string, List<ItemImprimirDto>>();

            if (dto.Destinos.Contains("principal", StringComparer.OrdinalIgnoreCase))
                porDestino["principal"] = dto.Items;

            foreach (var item in dto.Items)
            {
                var ub = item.Ubicacion?.ToLower() ?? "";
                if ((ub == "cocina" || ub == "barra") &&
                    dto.Destinos.Contains(ub, StringComparer.OrdinalIgnoreCase))
                {
                    if (!porDestino.TryGetValue(ub, out var lista))
                        porDestino[ub] = lista = [];
                    lista.Add(item);
                }
            }

            var resultados = new List<ResultadoImpresoraDto>();
            foreach (var (destino, items) in porDestino)
            {
                byte[] ticket = destino == "principal"
                    ? BuildCuenta(dto.Mesa, dto.Codigo, items, dto.Total, dto.MetodoPago)
                    : BuildTicket(destino, dto.Mesa, null, items);
                var (ok, error) = await EnviarAsync(destino, ticket);
                resultados.Add(new ResultadoImpresoraDto { Destino = destino, Ok = ok, Error = error });
            }
            return resultados;
        }

        public async Task<List<ResultadoImpresoraDto>> EnviarReciboAsync(ReciboImprimirDto dto)
        {
            var ticket = BuildRecibo(dto.Mesa, dto.Codigo, dto.Total, dto.MetodoPago);
            var resultados = new List<ResultadoImpresoraDto>();
            foreach (var destino in dto.Destinos)
            {
                var (ok, error) = await EnviarAsync(destino, ticket);
                resultados.Add(new ResultadoImpresoraDto { Destino = destino, Ok = ok, Error = error });
            }
            return resultados;
        }

        public Task EnviarCatalogoAsync(CatalogoImprimirDto dto)
        {
            logger.LogInformation("Catálogo recibido: {Total} productos", dto.Productos.Count);
            foreach (var p in dto.Productos)
                logger.LogInformation("  - {Nombre} ({Ubicacion})", p.Nombre, p.Ubicacion);
            return Task.CompletedTask;
        }

        // ── ESC/POS builders ─────────────────────────────────────────────────

        private byte[] BuildTicket(string destino, string mesa, string? ronda, List<ItemImprimirDto> items)
        {
            using var ms = new MemoryStream();
            ms.Write(INIT);
            ms.Write(ALIGN_CENTER); ms.Write(BIG); ms.Write(BOLD_ON);
            ms.Write(Enc.GetBytes(destino.ToUpper())); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF);
            ms.Write(BOLD_ON); ms.Write(Enc.GetBytes($"MESA: {mesa}")); ms.Write(LF); ms.Write(BOLD_OFF);
            if (!string.IsNullOrEmpty(ronda))
            { ms.Write(Enc.GetBytes(ronda)); ms.Write(LF); }
            ms.Write(ALIGN_LEFT);
            ms.Write(Enc.GetBytes($"Hora: {DateTime.Now:HH:mm  dd/MM/yyyy}")); ms.Write(LF);
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);

            foreach (var item in items)
            {
                ms.Write(BOLD_ON);
                ms.Write(Enc.GetBytes($"  {item.Cantidad}x {item.Nombre}")); ms.Write(LF);
                ms.Write(BOLD_OFF);
                if (!string.IsNullOrWhiteSpace(item.Nota))
                { ms.Write(Enc.GetBytes($"     >> {item.Nota}")); ms.Write(LF); }
            }

            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            ms.Write(LF); ms.Write(LF);
            ms.Write(CUT);
            return ms.ToArray();
        }

        private byte[] BuildTicketPrincipal(string mesa, string? ronda, List<ItemImprimirDto> items)
        {
            using var ms = new MemoryStream();
            ms.Write(INIT);
            ms.Write(ALIGN_CENTER); ms.Write(BIG); ms.Write(BOLD_ON);
            ms.Write(Enc.GetBytes("PRINCIPAL")); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF);
            ms.Write(BOLD_ON); ms.Write(Enc.GetBytes($"MESA: {mesa}")); ms.Write(LF); ms.Write(BOLD_OFF);
            if (!string.IsNullOrEmpty(ronda))
            { ms.Write(Enc.GetBytes(ronda)); ms.Write(LF); }
            ms.Write(ALIGN_LEFT);
            ms.Write(Enc.GetBytes($"Hora: {DateTime.Now:HH:mm  dd/MM/yyyy}")); ms.Write(LF);
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);

            decimal total = 0;
            foreach (var item in items)
            {
                ms.Write(BOLD_ON);
                decimal precio = item.Precio ?? 0;
                if (precio > 0)
                {
                    decimal subtotal = precio * item.Cantidad;
                    total += subtotal;
                    var izq = $"  {item.Cantidad}x {item.Nombre}";
                    var der = $"Bs/{subtotal:F2}";
                    int pad = Math.Max(1, 32 - izq.Length - der.Length);
                    ms.Write(Enc.GetBytes(izq + new string(' ', pad) + der)); ms.Write(LF);
                    if (item.Cantidad > 1)
                    {
                        ms.Write(BOLD_OFF);
                        ms.Write(Enc.GetBytes($"     Bs/{precio:F2} c/u")); ms.Write(LF);
                        ms.Write(BOLD_ON);
                    }
                }
                else
                {
                    ms.Write(Enc.GetBytes($"  {item.Cantidad}x {item.Nombre}")); ms.Write(LF);
                }
                ms.Write(BOLD_OFF);
                if (!string.IsNullOrWhiteSpace(item.Nota))
                { ms.Write(Enc.GetBytes($"     >> {item.Nota}")); ms.Write(LF); }
            }

            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            if (total > 0)
            {
                ms.Write(BOLD_ON); ms.Write(ALIGN_CENTER); ms.Write(BIG);
                ms.Write(Enc.GetBytes($"TOTAL  Bs/ {total:F2}")); ms.Write(LF);
                ms.Write(NORMAL); ms.Write(BOLD_OFF); ms.Write(ALIGN_LEFT);
            }
            ms.Write(LF); ms.Write(LF);
            ms.Write(CUT);
            return ms.ToArray();
        }

        private byte[] BuildCuenta(string mesa, string codigo, List<ItemImprimirDto> items, decimal total, string? metodoPago)
        {
            using var ms = new MemoryStream();
            ms.Write(INIT);
            ms.Write(ALIGN_CENTER); ms.Write(BIG); ms.Write(BOLD_ON);
            ms.Write(Enc.GetBytes("Kafe Yana")); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF);
            if (!string.IsNullOrEmpty(mesa) && mesa != codigo)
            { ms.Write(Enc.GetBytes(mesa)); ms.Write(LF); }
            ms.Write(Enc.GetBytes($"Cod: {codigo}")); ms.Write(LF);
            ms.Write(Enc.GetBytes($"Hora: {DateTime.Now:HH:mm  dd/MM/yyyy}")); ms.Write(LF);
            ms.Write(ALIGN_LEFT);
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);

            foreach (var item in items)
            {
                decimal precio   = item.Precio ?? 0;
                decimal subtotal = item.Total ?? precio * item.Cantidad;
                var izq = $"  {item.Cantidad}x {item.Nombre}";
                var der = $"Bs/{subtotal:F2}";
                int pad = Math.Max(1, 32 - izq.Length - der.Length);
                ms.Write(Enc.GetBytes(izq + new string(' ', pad) + der)); ms.Write(LF);
                if (item.Cantidad > 1)
                { ms.Write(Enc.GetBytes($"    Bs/{precio:F2} c/u")); ms.Write(LF); }
            }

            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            ms.Write(BOLD_ON); ms.Write(ALIGN_CENTER); ms.Write(BIG);
            ms.Write(Enc.GetBytes($"TOTAL  Bs/ {total:F2}")); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF); ms.Write(ALIGN_LEFT);
            if (!string.IsNullOrEmpty(metodoPago))
            {
                var label = MetodoLabel.GetValueOrDefault(metodoPago, metodoPago.ToUpper());
                ms.Write(Enc.GetBytes($"Pago: {label}")); ms.Write(LF);
            }
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            ms.Write(ALIGN_CENTER); ms.Write(Enc.GetBytes("Gracias por su visita!")); ms.Write(LF);
            ms.Write(LF); ms.Write(LF);
            ms.Write(CUT);
            return ms.ToArray();
        }

        private byte[] BuildRecibo(string mesa, string codigo, decimal total, string? metodoPago)
        {
            using var ms = new MemoryStream();
            ms.Write(INIT);
            ms.Write(ALIGN_CENTER); ms.Write(BIG); ms.Write(BOLD_ON);
            ms.Write(Enc.GetBytes("Kafe Yana")); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF);
            if (!string.IsNullOrEmpty(mesa) && mesa != codigo)
            { ms.Write(Enc.GetBytes(mesa)); ms.Write(LF); }
            ms.Write(Enc.GetBytes($"Cod: {codigo}")); ms.Write(LF);
            ms.Write(Enc.GetBytes($"Hora: {DateTime.Now:HH:mm  dd/MM/yyyy}")); ms.Write(LF);
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            ms.Write(BOLD_ON); ms.Write(ALIGN_CENTER); ms.Write(BIG);
            ms.Write(Enc.GetBytes($"TOTAL  Bs/ {total:F2}")); ms.Write(LF);
            ms.Write(NORMAL); ms.Write(BOLD_OFF); ms.Write(ALIGN_LEFT);
            if (!string.IsNullOrEmpty(metodoPago))
            {
                var label = MetodoLabel.GetValueOrDefault(metodoPago, metodoPago.ToUpper());
                ms.Write(Enc.GetBytes($"Pago: {label}")); ms.Write(LF);
            }
            ms.Write(Enc.GetBytes(new string('=', 32))); ms.Write(LF);
            ms.Write(ALIGN_CENTER); ms.Write(Enc.GetBytes("Gracias por su visita!")); ms.Write(LF);
            ms.Write(LF); ms.Write(LF);
            ms.Write(CUT);
            return ms.ToArray();
        }

        // ── TCP send ──────────────────────────────────────────────────────────

        private async Task<(bool ok, string? error)> EnviarAsync(string destino, byte[] data)
        {
            if (_opts.DevMode)
            {
                logger.LogInformation("[SIM:{Destino}]\n{Texto}", destino.ToUpper(), DecodeTicket(data));
                return (true, null);
            }

            if (!_opts.Destinos.TryGetValue(destino.ToLower(), out var cfg))
                return (false, "Destino no configurado");

            // Serializar por destino: una sola operación de envío a la vez por impresora.
            await GetLock(destino).WaitAsync();
            try
            {
                string? ultimoError = null;
                for (int intento = 1; intento <= 3; intento++)
                {
                    try
                    {
                        using var tcp = new TcpClient();
                        tcp.SendTimeout  = 3000;
                        tcp.ReceiveTimeout = 3000;
                        await tcp.ConnectAsync(cfg.Ip, cfg.Port);
                        var stream = tcp.GetStream();
                        await stream.WriteAsync(data);
                        await stream.FlushAsync();
                        logger.LogInformation("Enviado OK a {Ip}:{Port} (intento {N})", cfg.Ip, cfg.Port, intento);
                        return (true, null);
                    }
                    catch (Exception ex)
                    {
                        ultimoError = ex.Message;
                        logger.LogWarning("Intento {N}/3 fallido -> {Ip}:{Port} — {Error}", intento, cfg.Ip, cfg.Port, ex.Message);
                        if (intento < 3) await Task.Delay(500);
                    }
                }

                logger.LogError("Todos los reintentos fallaron -> {Ip}:{Port} — {Error}", cfg.Ip, cfg.Port, ultimoError);
                return (false, ultimoError);
            }
            finally
            {
                GetLock(destino).Release();
            }
        }

        private static string DecodeTicket(byte[] data)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i < data.Length)
            {
                byte b = data[i];
                if (b == 0x1B)
                {
                    byte cmd = i + 1 < data.Length ? data[i + 1] : (byte)0;
                    i += cmd is 0x40 or 0x45 or 0x61 ? 3 : 2;
                    continue;
                }
                if (b == 0x1D) { i += 4; continue; }
                if (b == 0x0A) sb.Append('\n');
                else if (b >= 0x20 && b < 0x7F) sb.Append((char)b);
                i++;
            }
            return sb.ToString().Trim();
        }
    }
}
