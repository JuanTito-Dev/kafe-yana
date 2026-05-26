using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios
{
    public class VentaServices(
        IUnitWork _db,
        IPuntosService _puntos,
        IPromocionPermanenteVentaService _promocionPermanenteVenta,
        IPromocionPermanenteDescuentoService _promocionDescuento,
        IPromocionPermanenteProductoGratisService _productoGratis) : IVentaServices
    {
        public async Task<ResultadoProcesarVenta> ProcesarVenta(DtoVentaPedido datos, string cajero)
        {
            var pedido = await _db.Pedidos.TraerPedido(datos.Id_Pedido);
            if (pedido is null)
                throw new InventarioException("Pedido no encontrado");

            var cliente = pedido.Cliente;
            if (pedido.Id_Cliente is null || pedido.Id_Cliente != datos.Id_Cliente)
            {
                cliente = await _db.clientes.FindByIdAsync(datos.Id_Cliente);
                if (cliente is null)
                    throw new InventarioException("Cliente no encontrado");
            }

            var anio = DateTime.UtcNow.Year;
            var numeroVenta = await _db.ventas.SiguienteNumeroVentaAsync();
            var codigoVenta = $"VTA-{anio}-{numeroVenta:D3}";

            var detallesVenta  = new List<Detalle_venta>();
            var subtotal       = 0.00M;
            var productosCount = 0;
            var tieneCombo     = false;

            foreach (var ronda in pedido.Rondas)
            {
                foreach (var detalle in ronda.Detalle)
                {
                    var totalDetalle = detalle.Precio * detalle.Cantidad;

                    detallesVenta.Add(new Detalle_venta
                    {
                        Nombre   = detalle.Nombre_Producto,
                        Cantidad = detalle.Cantidad,
                        Precio   = detalle.Precio,
                        Total    = totalDetalle
                    });

                    subtotal += totalDetalle;
                    productosCount++;

                    if (detalle.ItemsCombo.Count > 0)
                        tieneCombo = true;
                }
            }

            ResultadoAplicacionDescuentoPromocion? descuento = null;
            var totalCobrar = subtotal;

            if (datos.AplicarDescuentos)
            {
                descuento = await _promocionDescuento.AplicarDescuentoAsync(cliente, subtotal, codigoVenta);
                if (descuento is null)
                    throw new InventarioException("No hay descuentos aplicables para este pedido y cliente.");

                totalCobrar = descuento.TotalConDescuento;
            }

            if (datos.Pagos.Total != totalCobrar)
            {
                var esperado = datos.AplicarDescuentos
                    ? $"total con descuento ({totalCobrar:F2})"
                    : $"total del pedido ({totalCobrar:F2})";

                throw new InventarioException($"El total de los pagos no coincide con el {esperado}.");
            }

            var venta = new Venta
            {
                Codigo                          = codigoVenta,
                Fecha                           = DateTime.UtcNow,
                Cliente                         = cliente!.Nombre,
                Id_Cliente                      = cliente.Id,
                Cajero                          = cajero,
                Productos                       = productosCount,
                PagoEfectivo                    = datos.Pagos.Efectivo,
                PagoTarjeta                     = datos.Pagos.Tarjeta,
                PagoQr                          = datos.Pagos.Qr,
                Estado                          = "Finalizada",
                Subtotal                        = subtotal,
                MontoDescuento                  = descuento?.MontoDescuento ?? 0,
                PorcentajeDescuento             = descuento?.PorcentajeDescuento,
                Id_PromocionPermanenteDescuento = descuento?.IdPromocion,
                NombrePromocionDescuento        = descuento?.NombrePromocion,
                Total                           = totalCobrar,
                Detalles                        = detallesVenta
            };

            await _db.Pedidos.Remove(pedido);

            var puntosPorVenta = await _puntos.CalcularYAplicarPuntosAsync(cliente, subtotal, tieneCombo, codigoVenta);

            var promocionPermanente = await _promocionPermanenteVenta.ProcesarAlFinalizarVentaAsync(
                cliente, subtotal, codigoVenta);

            await _productoGratis.RegistrarProgresoPostVentaAsync(cliente.Id, subtotal);

            cliente.RegistrarCompra();

            return new ResultadoProcesarVenta
            {
                Venta               = venta,
                PuntosPorVenta      = puntosPorVenta,
                PromocionPermanente = promocionPermanente,
                DescuentoPromocion  = descuento
            };
        }
    }
}
