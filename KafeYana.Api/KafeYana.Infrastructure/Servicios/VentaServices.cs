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
        public async Task<Venta> ProcesarVenta(int Id_Pedido, int? Id_Cliente, string cajero, string tipoPago)
        {
            var pedido = await _db.Pedidos.TraerPedido(Id_Pedido);
            if (pedido == null) 
                throw new InventarioException("Pedido no encontrado");

            var cliente = pedido.Cliente;
            if (pedido.Id_Cliente == null || pedido.Id_Cliente != Id_Cliente)
            {
                cliente = await _db.clientes.FindByIdAsync(Id_Cliente ?? 0);
                if (cliente == null) 
                    throw new InventarioException("Cliente no encontrado");
            }

            int productosCount = 0;
            var detalleVenta = new List<Detalle_venta>();
            decimal subtotal = 0.00m;

            // Procesar todas las rondas y detalles de forma optimizada
            foreach (var ronda in pedido.Rondas)
            {
                foreach (var detalle in ronda.Detalle)
                {
                    if (detalle.producto == null)
                        throw new InventarioException($"Producto {detalle.Id_Producto} no encontrado");

                    // Obtener opciones del detalle si existen
                    var opciones = detalle.Opciones ?? new List<Detalle_Ronda_Opcion>();

                    try
                    {
                        // Descontar stock según tipo de producto
                        await ProcesarDescount(detalle.Id_Producto, detalle.Cantidad, detalle.producto.Tipo, opciones);

                        productosCount++;
                        
                        // Crear detalle de venta
                        var nombreDetalle = ConstruirNombreDetalle(detalle, opciones);
                        detalleVenta.Add(new Detalle_venta
                        {
                            Nombre = nombreDetalle,
                            Cantidad = detalle.Cantidad,
                            Precio = detalle.Precio,
                            Total = detalle.Precio * detalle.Cantidad
                        });

                        subtotal += detalle.Precio * detalle.Cantidad;
                    }
                    catch (Exception ex)
                    {
                        throw new InventarioException($"Error procesando producto {detalle.Nombre_Producto}: {ex.Message}");
                    }
                }
            }

            await _db.Pedidos.Remove(pedido);

            return new Venta
            {
                Fecha = DateTime.UtcNow,
                Cliente = cliente!.Nombre,
                Cajero = cajero,
                Productos = productosCount,
                Pago = tipoPago,
                Estado = "Finalizada",
                Subtotal = subtotal,
                Total = subtotal,
                Detalles = detalleVenta
            };
        }

        /// <summary>
        /// Procesa el descuento según el tipo de producto y opciones
        /// </summary>
        private async Task ProcesarDescount(int idProducto, int cantidad, string tipoProducto, List<Detalle_Ronda_Opcion> opciones)
        {
            switch (tipoProducto)
            {
                case TiposProductos.Comprado:
                    await DescontarComprado(idProducto, cantidad);
                    break;

                case TiposProductos.Elaborado:
                    await DescontarElaborado(idProducto, cantidad, opciones);
                    break;

                case TiposProductos.Promocion:
                    await DescontarPromocion(idProducto, cantidad);
                    break;

                default:
                    throw new InventarioException($"Tipo de producto desconocido: {tipoProducto}");
            }
        }

        /// <summary>
        /// Construye el nombre del detalle con las opciones aplicadas
        /// </summary>
        private string ConstruirNombreDetalle(Detalle_ronda detalle, List<Detalle_Ronda_Opcion> opciones)
        {
            if (opciones == null || opciones.Count == 0)
                return detalle.Nombre_Producto;

            var nombresOpciones = opciones
                .Where(x => x.Opcion != null)
                .Select(x => x.Opcion!.Nombre)
                .ToList();

            return nombresOpciones.Count > 0
                ? $"{detalle.Nombre_Producto} ({string.Join(", ", nombresOpciones)})"
                : detalle.Nombre_Producto;
        }

        public async Task DescontarComprado(int id, int cantidad)
        {
            var producto = await _db.productos.TraerProducto(id, comprado: true);

            if (producto is null) 
                throw new InventarioException($"Producto comprado no encontrado: {id}");

            if (producto.Comprado.Stock_actual < cantidad) 
                throw new InventarioException($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Comprado.Stock_actual}, Solicitado: {cantidad}");

            producto.Comprado.Stock_actual -= cantidad;
        }

        public async Task DescontarElaborado(int id, int cantidad, List<Detalle_Ronda_Opcion> opciones)
        {
            if (await _db.elaborados.EsProducible(id))
            {
                var elaborado = await _db.elaborados.TraerElaborado(id);
                if (elaborado is null) 
                    throw new InventarioException($"Elaborado no encontrado: {id}");
                
                elaborado.Stock_actual -= cantidad;
            }
            else
            {
                var elaborado = await _db.elaborados.TraerElaborado(id, withreceta: true);
                if (elaborado is null) 
                    throw new InventarioException($"Elaborado no encontrado: {id}");
                
                if (elaborado.Receta is null) 
                    return;

                // Procesar múltiples opciones
                var opcionIds = opciones?.Select(x => x.Id_Opcion).ToList() ?? new List<int>();
                var opcionesEntity = new List<Opcion>();

                if (opcionIds.Count > 0)
                {
                    opcionesEntity = await _db.opciones.Query()
                        .Where(x => opcionIds.Contains(x.Id))
                        .Include(x => x.Ajustes)
                            .ThenInclude(x => x.InsumoBase)
                        .Include(x => x.Ajustes)
                            .ThenInclude(x => x.InsumoNuevo)
                        .ToListAsync();
                }

                // Insumos a omitir (reemplazos)
                var insumosOmitidos = opcionesEntity
                    .SelectMany(x => x.Ajustes)
                    .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
                    .Select(x => x.Id_Insumo)
                    .ToHashSet();

                // Descontar insumos de la receta base
                foreach (var detalleReceta in elaborado.Receta.Detalles)
                {
                    if (insumosOmitidos.Contains(detalleReceta.Id_insumo)) 
                        continue;

                    decimal cantidadFinal = detalleReceta.Cantidad * cantidad * (1 + (detalleReceta.Merma / 100));

                    // Aplicar modificaciones de todas las opciones
                    var totalModificaciones = opcionesEntity
                        .SelectMany(x => x.Ajustes)
                        .Where(x => x.Id_Insumo == detalleReceta.Id_insumo && x.TipoAjuste == TiposAjuste.Modificacion)
                        .Sum(x => x.Cantidad);

                    cantidadFinal += totalModificaciones * cantidad;

                    if (detalleReceta.Insumo.Stock_actual < cantidadFinal)
                        throw new InventarioException($"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}");

                    detalleReceta.Insumo.Stock_actual -= (int)cantidadFinal;
                }

                // Descontar insumos nuevos de los reemplazos (optimizado)
                var reemplazos = opcionesEntity
                    .SelectMany(x => x.Ajustes)
                    .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
                    .GroupBy(x => x.Id_InsumoNuevo)
                    .ToList();

                foreach (var grupo in reemplazos)
                {
                    var cantidadTotalReemplazo = grupo.Sum(x => x.Cantidad) * cantidad;
                    var insumoNuevo = grupo.First().InsumoNuevo;

                    if (insumoNuevo.Stock_actual < cantidadTotalReemplazo)
                        throw new InventarioException($"Stock insuficiente para insumo {insumoNuevo.Nombre}");

                    insumoNuevo.Stock_actual -= (int)cantidadTotalReemplazo;
                }
            }
        }

        public async Task DescontarPromocion(int id, int cantidad)
        {
            var promocion = await _db.Combo.TraerPromocion(id);
            if (promocion is null) 
                throw new InventarioException($"Promoción no encontrada: {id}");

            foreach (var detalle in promocion.Detalles)
            {
                if (detalle.Producto is null) 
                    throw new InventarioException($"Producto no encontrado en promoción {id}");

                var cantidadTotal = detalle.Cantidad * cantidad;

                switch (detalle.Producto.Tipo)
                {
                    case TiposProductos.Comprado:
                        await DescontarComprado(detalle.Id_Producto, cantidadTotal);
                        break;

                    case TiposProductos.Elaborado:
                        await DescontarElaborado(detalle.Id_Producto, cantidadTotal, new List<Detalle_Ronda_Opcion>());
                        break;
                }
            }
        }
    }
}
