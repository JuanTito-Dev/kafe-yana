using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Servicios.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Cobro parcial e independiente de parte de lo consumido en un pedido
    /// (mesa o para llevar): "sub-venta". Ver feature-venta.md.
    /// </summary>
    public class SubVentaService(
        IUnitWork _db,
        AppDbContext _dbContext,
        IVentaServices _venta,
        IFacturaSiatEnvioService _facturaSiatEnvio,
        ILogger<SubVentaService> logger) : ISubVentaService
    {
        // classId fijo para namespacear el advisory lock — evita colisiones si en
        // el futuro otra feature adopta pg_advisory_xact_lock con ids simples.
        private const int LockClassId = 778899;

        public async Task<ResultadoCobroPedidoDto> CrearSubVentaAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            string origenVenta,
            Action liberarPedido,
            int? idMesa,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cajero))
                throw new VentaException("Usuario cajero no identificado.");

            var items = datos.ItemsCubiertos;
            if (items is null || items.Count == 0)
                throw new VentaException("Debe especificar al menos un producto a cobrar.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                // Bloqueo real por pedido: dos cobros parciales concurrentes del
                // mismo pedido se serializan acá — el segundo espera a que el
                // primero confirme o revierta antes de leer cuánto queda pendiente.
                // Se libera automáticamente al terminar la transacción (commit o
                // rollback), sin riesgo de quedar colgado si la request revienta.
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0}, {1})",
                    new object[] { LockClassId, datos.Id_Pedido }, ct);

                var pedido = await _dbContext.Pedidos
                    .Include(p => p.Rondas.OrderBy(r => r.Id))
                        .ThenInclude(r => r.Detalle)
                    .FirstOrDefaultAsync(p => p.Id == datos.Id_Pedido, ct)
                    ?? throw new InventarioException("Pedido no encontrado.");

                var subVenta = new SubVenta
                {
                    Id_Pedido = pedido.Id,
                    Fecha = DateTime.UtcNow,
                    Cajero = cajero,
                    CodigoMetodoPago = datos.Pagos.Lineas.OrderByDescending(l => l.Monto).First().CodigoMetodoPago,
                };

                foreach (var item in items)
                {
                    if (item.Cantidad <= 0)
                        throw new VentaException("La cantidad a cobrar debe ser mayor a cero.");

                    // Todas las filas de este producto, across TODAS las rondas activas
                    // del pedido, en orden FIFO (ronda más antigua primero).
                    var filas = pedido.Rondas
                        .SelectMany(r => r.Detalle.Select(d => (Ronda: r, Detalle: d)))
                        .Where(x => x.Detalle.Id_Producto == item.Id_Producto)
                        .OrderBy(x => x.Ronda.Id)
                        .ThenBy(x => x.Detalle.Id)
                        .ToList();

                    if (filas.Count == 0)
                        throw new VentaException($"El producto {item.Id_Producto} no está en ninguna ronda activa de este pedido.");

                    var nombreProducto = filas[0].Detalle.Nombre_Producto;
                    var pendienteTotal = filas.Sum(f => f.Detalle.Cantidad - f.Detalle.CantidadDescontada);

                    if (item.Cantidad > pendienteTotal)
                    {
                        throw new VentaException(
                            $"No hay suficiente cantidad pendiente de '{nombreProducto}': " +
                            $"disponible={pendienteTotal}, solicitado={item.Cantidad}.");
                    }

                    var restante = item.Cantidad;
                    foreach (var (ronda, detalle) in filas)
                    {
                        if (restante == 0) break;

                        var disponibleEnFila = detalle.Cantidad - detalle.CantidadDescontada;
                        if (disponibleEnFila <= 0) continue;

                        var tomar = Math.Min(disponibleEnFila, restante);
                        // La fila NUNCA se borra: se descuenta y, si llega a 0, queda
                        // en 0 para historial de cocina/auditoría.
                        detalle.CantidadDescontada += tomar;

                        subVenta.Detalles.Add(new SubVentaDetalle
                        {
                            Id_Producto = detalle.Id_Producto,
                            Nombre_Producto = detalle.Nombre_Producto,
                            Cantidad = tomar,
                            Precio = detalle.Precio,
                            Codigo = detalle.Codigo,
                            CodigoSin = detalle.CodigoSin,
                            CodigoUnidadMedida = detalle.CodigoUnidadMedida,
                            OrigenRondaId = ronda.Id,
                        });

                        restante -= tomar;
                    }
                }

                subVenta.Monto = subVenta.Detalles.Sum(d => d.Precio * d.Cantidad);

                if (datos.Pagos.Total != subVenta.Monto)
                {
                    throw new InventarioException(
                        $"El total de los pagos ({datos.Pagos.Total:F2}) no coincide con el monto a cobrar ({subVenta.Monto:F2}).");
                }

                var pendienteTotalPedido = pedido.Rondas
                    .SelectMany(r => r.Detalle)
                    .Sum(d => d.Cantidad - d.CantidadDescontada);

                var mesaCerrada = pendienteTotalPedido <= 0;
                subVenta.EsPagoFinal = mesaCerrada;

                ResultadoEnvioFacturaSiatDto? envioSiat = null;

                // Se crea una Venta SIEMPRE, facture o no — igual que el camino de
                // cobro completo (EjecutarCobroAsync). "Factura" solo decide si además
                // se emite al SIAT; de lo contrario nunca hay fila en Venta y el cobro
                // desaparece del historial de ventas (que solo lee esa tabla).
                var resultadoVenta = datos.Factura
                    ? await _venta.ProcesarVentaDesdeSubVentaAsync(subVenta, datos, cajero)
                    : await _venta.ProcesarVentaSinFacturaDesdeSubVentaAsync(subVenta, datos, cajero);

                if (datos.Factura)
                {
                    envioSiat = await _facturaSiatEnvio.EnviarVentaAsync(resultadoVenta.Venta, ct);

                    if (!FacturaSiatCobroPolicy.PermiteCompletarCobro(envioSiat))
                        throw new VentaException(FacturaSiatCobroPolicy.MensajeRechazoCobro(envioSiat));

                    subVenta.Facturada = true;
                }

                await _db.ventas.Crear(resultadoVenta.Venta);
                _dbContext.SubVentas.Add(subVenta);
                await _dbContext.SaveChangesAsync(ct);

                subVenta.Id_Venta = resultadoVenta.Venta.Id;

                if (mesaCerrada)
                    liberarPedido();

                caja.RegistrarVenta(datos.Pagos.Lineas.Select(l => (l.CodigoMetodoPago, l.Monto)));

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var totalAbonado = await _dbContext.SubVentas
                    .Where(s => s.Id_Pedido == pedido.Id)
                    .SumAsync(s => s.Monto, ct);
                var saldo = Math.Max(0, pedido.Total - totalAbonado);

                var pedidoActualizado = new DtoPedidoActualizado
                {
                    Id_Pedido = pedido.Id,
                    Total = pedido.Total,
                    TotalAbonado = totalAbonado,
                    Saldo = saldo,
                    Detalles = pedido.Rondas.SelectMany(r => r.Detalle).Select(d => new DtoDetalleEstado
                    {
                        Id_Detalle = d.Id,
                        Id_Producto = d.Id_Producto,
                        CantidadDescontada = d.CantidadDescontada,
                    }).ToList(),
                };

                return new ResultadoCobroPedidoDto
                {
                    OrigenVenta = origenVenta,
                    IdMesa = idMesa,
                    EsAbono = true,
                    MontoCubierto = subVenta.Monto,
                    PedidoActualizado = pedidoActualizado,
                    MesaCerrada = mesaCerrada,
                    EnvioSiat = envioSiat,
                    // Si esta sub-venta facturó, el controller usa esto para
                    // devolver los mismos campos de factura que el cobro
                    // completo (VentaId, NumeroFactura, CUF, etc.) — sin esto
                    // la pantalla de éxito no puede ofrecer "Imprimir factura".
                    Resultado = resultadoVenta,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error creando sub-venta (pedidoId={PedidoId}): {Error}", datos.Id_Pedido, ex.Message);
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<DtoPedidoActualizado> RevertirSubVentaAsync(int subVentaId, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var subVenta = await _dbContext.SubVentas
                    .Include(s => s.Detalles)
                    .FirstOrDefaultAsync(s => s.Id == subVentaId, ct)
                    ?? throw new VentaException("Sub-venta no encontrada.");

                if (subVenta.Id_Venta.HasValue)
                    throw new VentaException("No se puede revertir una sub-venta con venta ya registrada; anule la venta primero.");

                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0}, {1})",
                    new object[] { LockClassId, subVenta.Id_Pedido }, ct);

                var idsRondas = subVenta.Detalles
                    .Where(d => d.OrigenRondaId.HasValue)
                    .Select(d => d.OrigenRondaId!.Value)
                    .Distinct()
                    .ToList();

                var detallesOrigen = await _dbContext.Detalle_rondas
                    .Where(d => idsRondas.Contains(d.Id_Ronda))
                    .ToListAsync(ct);

                foreach (var linea in subVenta.Detalles)
                {
                    var detalle = detallesOrigen.FirstOrDefault(d =>
                        d.Id_Ronda == linea.OrigenRondaId && d.Id_Producto == linea.Id_Producto);
                    if (detalle is not null)
                        detalle.CantidadDescontada = Math.Max(0, detalle.CantidadDescontada - linea.Cantidad);
                }

                _dbContext.SubVentaDetalles.RemoveRange(subVenta.Detalles);
                _dbContext.SubVentas.Remove(subVenta);

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var pedido = await _dbContext.Pedidos
                    .Include(p => p.Rondas)
                        .ThenInclude(r => r.Detalle)
                    .FirstOrDefaultAsync(p => p.Id == subVenta.Id_Pedido, ct)
                    ?? throw new InventarioException("Pedido no encontrado.");

                var totalAbonado = await _dbContext.SubVentas
                    .Where(s => s.Id_Pedido == pedido.Id)
                    .SumAsync(s => s.Monto, ct);

                return new DtoPedidoActualizado
                {
                    Id_Pedido = pedido.Id,
                    Total = pedido.Total,
                    TotalAbonado = totalAbonado,
                    Saldo = Math.Max(0, pedido.Total - totalAbonado),
                    Detalles = pedido.Rondas.SelectMany(r => r.Detalle).Select(d => new DtoDetalleEstado
                    {
                        Id_Detalle = d.Id,
                        Id_Producto = d.Id_Producto,
                        CantidadDescontada = d.CantidadDescontada,
                    }).ToList(),
                };
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<ResultadoEnvioFacturaSiatDto?> FacturarSubVentaAsync(
            int subVentaId, DtoFacturarSubVenta datos, string cajero, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var subVenta = await _db.subventas.GetByIdConDetallesAsync(subVentaId)
                    ?? throw new VentaException("Sub-venta no encontrada.");

                if (subVenta.Facturada)
                    throw new VentaException("Esta sub-venta ya tiene una factura emitida; anúlela para reemitir.");

                var datosVenta = new DtoVentaPedido
                {
                    Id_Pedido = subVenta.Id_Pedido,
                    Id_Cliente = datos.Id_Cliente,
                    Pagos = new DtoPagos
                    {
                        Lineas = [new DtoPagoLinea { CodigoMetodoPago = subVenta.CodigoMetodoPago, Monto = subVenta.Monto }]
                    },
                    Factura = true,
                    CodigoTipoDocumento = datos.CodigoTipoDocumento,
                    Nombre = datos.Nombre,
                    Dni = datos.Dni,
                    Complemento = datos.Complemento,
                    CodigoSucursal = datos.CodigoSucursal,
                    CodigoPuntoVenta = datos.CodigoPuntoVenta,
                    CodigoPaisOrigen = datos.CodigoPaisOrigen,
                };

                var resultadoVenta = await _venta.ProcesarVentaDesdeSubVentaAsync(subVenta, datosVenta, cajero);
                var envioSiat = await _facturaSiatEnvio.EnviarVentaAsync(resultadoVenta.Venta, ct);

                if (!FacturaSiatCobroPolicy.PermiteCompletarCobro(envioSiat))
                    throw new VentaException(FacturaSiatCobroPolicy.MensajeRechazoCobro(envioSiat));

                await _db.ventas.Crear(resultadoVenta.Venta);
                await _dbContext.SaveChangesAsync(ct);

                subVenta.Facturada = true;
                subVenta.Id_Venta = resultadoVenta.Venta.Id;
                await _dbContext.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
                return envioSiat;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error facturando sub-venta {Id}: {Error}", subVentaId, ex.Message);
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<List<DtoSubVentaPendiente>> GetPendientesFacturarAsync(
            int? idMesa = null, int? idParaLlevar = null, CancellationToken ct = default)
        {
            var pendientes = await _db.subventas.GetPendientesFacturarAsync(idMesa, idParaLlevar);
            return pendientes.Select(MapearResumen).ToList();
        }

        public async Task<List<DtoSubVentaPendiente>> GetPorPedidoAsync(int idPedido, CancellationToken ct = default)
        {
            var subVentas = await _db.subventas.GetByPedidoAsync(idPedido);
            return subVentas.Select(MapearResumen).ToList();
        }

        private static DtoSubVentaPendiente MapearResumen(SubVenta s) => new()
        {
            Id = s.Id,
            Fecha = s.Fecha,
            Monto = s.Monto,
            PedidoId = s.Id_Pedido,
            Origen = s.Pedido?.Mesa?.Nombre ?? (s.Pedido?.ParaLlevar is not null ? "Para llevar" : "—"),
            Cajero = s.Cajero,
            CantidadLineas = s.Detalles.Count,
            Facturada = s.Facturada,
            EsPagoFinal = s.EsPagoFinal,
            IdVenta = s.Id_Venta,
            CodigoMetodoPago = s.CodigoMetodoPago,
            Detalles = s.Detalles.Select(d => new DtoSubVentaDetalleResumen
            {
                NombreProducto = d.Nombre_Producto,
                Cantidad = d.Cantidad,
                Precio = d.Precio,
            }).ToList(),
        };
    }
}
