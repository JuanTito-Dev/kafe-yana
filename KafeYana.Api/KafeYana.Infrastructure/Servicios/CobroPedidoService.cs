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
        IFacturaSiatEnvioService _facturaSiatEnvio) : ICobroPedidoService
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
                    envioSiat = await _facturaSiatEnvio.EnviarVentaAsync(resultado.Venta, ct);

                    if (!FacturaSiatCobroPolicy.PermiteCompletarCobro(envioSiat))
                        throw new VentaException(FacturaSiatCobroPolicy.MensajeRechazoCobro(envioSiat));
                }

                await _db.ventas.Crear(resultado.Venta);
                liberarPedido();
                caja.RegistrarVenta(datos.Pagos.Efectivo, datos.Pagos.Tarjeta, datos.Pagos.Qr);

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
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
