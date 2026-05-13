using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios
{
    public class VentaServices(IUnitWork _db) : IVentaServices
    {
        public async Task<Venta> ProcesarVenta(int Id_Pedido, int Id_Cliente, string cajero, DtoPagos pagos)
        {
            var pedido = await _db.Pedidos.TraerPedido(Id_Pedido);
            if (pedido is null)
                throw new InventarioException("Pedido no encontrado");

            var cliente = pedido.Cliente;
            if (pedido.Id_Cliente is null || pedido.Id_Cliente != Id_Cliente)
            {
                cliente = await _db.clientes.FindByIdAsync(Id_Cliente);
                if (cliente is null)
                    throw new InventarioException("Cliente no encontrado");
            }

            var anio = DateTime.UtcNow.Year;
            var numeroVenta = await _db.ventas.ContarVentasDelAnio(anio) + 1;
            var codigoVenta = $"VTA-{anio}-{numeroVenta:D3}";

            var detallesVenta = new List<Detalle_venta>();
            var subtotal = 0.00M;
            var productosCount = 0;

            foreach (var ronda in pedido.Rondas)
            {
                foreach (var detalle in ronda.Detalle)
                {
                    var totalDetalle = detalle.Precio * detalle.Cantidad;

                    detallesVenta.Add(new Detalle_venta
                    {
                        Nombre = detalle.Nombre_Producto,
                        Cantidad = detalle.Cantidad,
                        Precio = detalle.Precio,
                        Total = totalDetalle
                    });

                    subtotal += totalDetalle;
                    productosCount++;
                }
            }

            await _db.Pedidos.Remove(pedido);

            return new Venta
            {
                Codigo = codigoVenta,
                Fecha = DateTime.UtcNow,
                Cliente = cliente!.Nombre,
                Cajero = cajero,
                Productos = productosCount,
                PagoEfectivo = pagos.Efectivo,
                PagoTarjeta = pagos.Tarjeta,
                PagoQr = pagos.Qr,
                Estado = "Finalizada",
                Subtotal = subtotal,
                Total = subtotal,
                Detalles = detallesVenta
            };
        }
    }
}
