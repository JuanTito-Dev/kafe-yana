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
    /// Servicio de cobro de pedidos. Tras el cobro NO se imprime la factura
    /// automáticamente; el frontend dispara el modal de impresión con la
    /// selección de impresoras cuando el cajero lo decide.
    /// </summary>
    public class CobroPedidoService(
        IUnitWork _db,
        AppDbContext _dbContext,
        IVentaServices _venta,
        ISubVentaService _subVenta,
        IFacturaSiatEnvioService _facturaSiatEnvio,
        ILogger<CobroPedidoService> logger) : ICobroPedidoService
    {

        public async Task<ResultadoCobroPedidoDto> CobrarPedidoActivoAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default)
        {
            var mesa = await _db.mesas.GetMesaPorPedidoAsync(datos.Id_Pedido);
            if (mesa is not null)
                return await CobrarMesaAsync(mesa.Id, datos, cajero, caja, ct);

            var paraLlevar = await _db.parallevar.GetPorPedidoActivoAsync(datos.Id_Pedido);
            if (paraLlevar is not null)
                return await CobrarParaLlevarAsync(datos, cajero, caja, ct);

            throw new VentaException("El pedido no está activo en ninguna mesa ni en para llevar.");
        }

        public async Task<ResultadoCobroPedidoDto> CobrarMesaAsync(
            int idMesa,
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default)
        {
            var mesa = await _db.mesas.GetMesaPedido(idMesa);
            if (mesa is null)
                throw new InventarioException("Mesa no existe.");

            if (!await _db.mesas.MesaConpedido(datos.Id_Pedido, idMesa))
                throw new InventarioException("El pedido no corresponde a la mesa.");

            if (await DebeUsarSubVentaAsync(datos))
            {
                return await _subVenta.CrearSubVentaAsync(
                    datos,
                    cajero,
                    caja,
                    mesa.Nombre,
                    liberarPedido: () =>
                    {
                        mesa.Disponible = true;
                        mesa.Id_Pedido = null;
                        mesa.pedido = null;
                    },
                    idMesa,
                    ct);
            }

            var pedido = await _db.Pedidos.FindByIdAsync(datos.Id_Pedido);
            if (pedido is null)
                throw new InventarioException("Pedido no encontrado.");

            return await EjecutarCobroAsync(
                datos,
                cajero,
                caja,
                mesa.Nombre,
                () =>
                {
                    mesa.Disponible = true;
                    mesa.Id_Pedido = null;
                    mesa.pedido = null;
                },
                ct,
                idMesa);
        }

        public async Task<ResultadoCobroPedidoDto> CobrarParaLlevarAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            CancellationToken ct = default)
        {
            var paraLlevar = await _db.parallevar.GetParaLlevarConPedido();
            if (paraLlevar is null)
                throw new InventarioException("Configuración para llevar no encontrada.");

            if (paraLlevar.Pedido is null)
                throw new InventarioException("No hay un pedido para llevar activo.");

            if (paraLlevar.Id_Pedido != datos.Id_Pedido)
                throw new InventarioException("El pedido no corresponde al pedido para llevar activo.");

            if (await DebeUsarSubVentaAsync(datos))
            {
                return await _subVenta.CrearSubVentaAsync(
                    datos,
                    cajero,
                    caja,
                    "Para llevar",
                    liberarPedido: () =>
                    {
                        paraLlevar.Disponible = true;
                        paraLlevar.Pedido = null;
                    },
                    idMesa: null,
                    ct);
            }

            return await EjecutarCobroAsync(
                datos,
                cajero,
                caja,
                "Para llevar",
                () =>
                {
                    paraLlevar.Disponible = true;
                    paraLlevar.Pedido = null;
                },
                ct);
        }

        public async Task<DtoPedidoActualizado> RevertirAbonoAsync(int abonoId, CancellationToken ct = default) =>
            await _subVenta.RevertirSubVentaAsync(abonoId, ct);

        /// <summary>
        /// Decide si este cobro debe ir por el camino de sub-venta (cobro parcial):
        /// el caller pidió explícitamente cobrar solo ciertos productos
        /// (<c>ItemsCubiertos</c> + <c>MantenerMesaAbierta</c>), o el pedido ya
        /// tiene sub-ventas previas — en ese caso, aunque el cajero use el botón de
        /// "cobrar todo", el resto DEBE seguir siendo una sub-venta (que naturalmente
        /// cierra la mesa si deja el pendiente en 0) para no perder el historial de
        /// cobros/facturas ya emitidas que el camino de cobro completo destruiría.
        /// Si el pedido no trae items explícitos pero ya tiene sub-ventas, se
        /// completan automáticamente con TODO lo pendiente de cada producto.
        /// </summary>
        private async Task<bool> DebeUsarSubVentaAsync(DtoVentaPedido datos)
        {
            if (datos.MantenerMesaAbierta && datos.ItemsCubiertos?.Count > 0)
                return true;

            var tieneSubVentasPrevias = await _dbContext.SubVentas.AnyAsync(s => s.Id_Pedido == datos.Id_Pedido);
            if (!tieneSubVentasPrevias)
                return false;

            if (datos.ItemsCubiertos is null || datos.ItemsCubiertos.Count == 0)
            {
                var pendientesPorProducto = await _dbContext.Detalle_rondas
                    .Where(d => d.ronda!.Id_Pedido == datos.Id_Pedido && d.Cantidad - d.CantidadDescontada > 0)
                    .GroupBy(d => d.Id_Producto)
                    .Select(g => new DtoItemProductoCobrar
                    {
                        Id_Producto = g.Key,
                        Cantidad = g.Sum(d => d.Cantidad - d.CantidadDescontada),
                    })
                    .ToListAsync();

                datos.ItemsCubiertos = pendientesPorProducto;
            }

            return true;
        }

        private async Task<ResultadoCobroPedidoDto> EjecutarCobroAsync(
            DtoVentaPedido datos,
            string cajero,
            Caja caja,
            string origenVenta,
            Action liberarPedido,
            CancellationToken ct,
            int? idMesa = null)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var resultado = await _venta.ProcesarVenta(datos, cajero);
                ResultadoEnvioFacturaSiatDto? envioSiat = null;

                if (datos.Factura)
                {
                    // Si VentaServices construyó la venta en modo contingencia
                    // (TipoEmision=2 + EventoSignificativoSiatId poblado), NO
                    // intentamos enviar al SIAT: el monitor de conectividad
                    // lo hará cuando detecte recuperación. Dejamos envioSiat=null
                    // y la venta persiste como EstadoSiat=Pendiente.
                    // Ver [[kafeyana-contingencia-siat]].
                    var esContingenciaOffline = resultado.Venta.TipoEmision == 2
                        && resultado.Venta.EventoSignificativoSiatId is not null;

                    if (!esContingenciaOffline)
                    {
                        envioSiat = await _facturaSiatEnvio.EnviarVentaAsync(resultado.Venta, ct);

                        if (!FacturaSiatCobroPolicy.PermiteCompletarCobro(envioSiat))
                            throw new VentaException(FacturaSiatCobroPolicy.MensajeRechazoCobro(envioSiat));
                    }
                    else
                    {
                        logger.LogInformation(
                            "Venta construida en modo contingencia (EventoId={Id}). "
                          + "Envío al SIAT se difiere para cuando se recupere la conexión.",
                            resultado.Venta.EventoSignificativoSiatId);
                    }
                }

                await _db.ventas.Crear(resultado.Venta);
                liberarPedido();
                caja.RegistrarVenta(datos.Pagos.Lineas.Select(l => (l.CodigoMetodoPago, l.Monto)));

                await _db.SaveUnitWork();
                await transaction.CommitAsync(ct);

                return new ResultadoCobroPedidoDto
                {
                    Resultado = resultado,
                    EnvioSiat = envioSiat,
                    OrigenVenta = origenVenta,
                    IdMesa = idMesa,
                };
            }
            catch (Exception ex)
            {
                // Logueamos la inner exception completa (PostgresException.Message, SqlState, etc.)
                // para que un cobro fallido muestre QUÉ columna/constraint reventó en consola.
                logger.LogError(ex,
                    "Error en cobro (pedidoId={PedidoId}, factura={Factura}): {Error}",
                    datos.Id_Pedido, datos.Factura, ex.Message);
                if (ex.InnerException is not null)
                {
                    logger.LogError("  Inner exception: {Inner}",
                        ex.InnerException.Message);
                    if (ex.InnerException.InnerException is not null)
                    {
                        logger.LogError("    Inner.inner: {InnerInner}",
                            ex.InnerException.InnerException.Message);
                    }
                }
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
