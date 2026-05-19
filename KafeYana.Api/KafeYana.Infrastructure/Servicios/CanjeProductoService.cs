using KafeYana.Application.Dtos.ProductoCanjeable;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios;

/// <summary>Canje por puntos — misma regla de inventario que pedidos (<c>Detalle_RondaService</c>),
/// con movimiento <c>Canje</c> en productos/promo.</summary>
public class CanjeProductoService(IUnitWork _unitWork) : ICanjeProductoService
{
    private const int CantidadCanjeUnitaria = 1;

    public async Task EjecutarCanjeAsync(DtoCanjeProducto dto)
    {
        var pc = await _unitWork.productosCanjeables.ObtenerParaCanjeAsync(dto.IdProductoCanjeable);

        if (pc is null)
            throw new InventarioException("Producto canjeable no encontrado.");

        if (!pc.Activo)
            throw new InventarioException("El producto canjeable está inactivo.");

        var cliente = await _unitWork.clientes.GetCliente(dto.IdCliente);

        if (cliente is null)
            throw new InventarioException("Cliente no encontrado.");

        if (cliente.Puntos < pc.Puntos)
            throw new InventarioException(
                $"Puntos insuficientes para este canje. Se requieren {pc.Puntos} y el cliente tiene {cliente.Puntos}.");

        if (pc.Producto is null)
            throw new InventarioException("Producto del canje no encontrado en catálogo.");

        var tipo = await _unitWork.productos.TraerTipoProducto(pc.Id_Producto);

        if (tipo is null)
            throw new InventarioException($"No se encontró el tipo para el producto {pc.Id_Producto}.");

        var referencia = $"CANJE-{pc.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var c = CantidadCanjeUnitaria;

        switch (tipo)
        {
            case TiposProductos.Comprado:
                await ProcesarCompradoCanje(pc.Id_Producto, c, referencia);
                break;

            case TiposProductos.Elaborado:
                await ProcesarElaboradoCanje(pc.Id_Producto, c, referencia);
                break;

            case TiposProductos.Promocion:
                await ProcesarComboCanje(pc.Id_Producto, c, referencia);
                break;

            default:
                throw new InventarioException($"Tipo de producto no admitido para canje: {tipo}");
        }

        cliente.DescontarPuntosPorCanje(pc.Puntos);
        await _unitWork.SaveUnitWork();
    }

    private async Task ProcesarCompradoCanje(int idProducto, int cantidad, string referencia)
    {
        var producto = await _unitWork.productos.TraerProducto(idProducto, comprado: true);

        if (producto?.Comprado is null)
            throw new InventarioException($"Producto comprado no encontrado: {idProducto}");

        if (producto.Comprado.Stock_actual < cantidad)
            throw new InventarioException(
                $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Comprado.Stock_actual}, solicitado: {cantidad}");

        var movimiento = producto.Comprado.Canje(cantidad, referencia);
        await _unitWork.movimientos.Crear(movimiento);
    }

    private async Task ProcesarElaboradoCanje(int idProducto, int cantidad, string referencia)
    {
        var elaborado = await _unitWork.elaborados.TraerElaborado(idProducto, withreceta: true);

        if (elaborado is null)
            throw new InventarioException($"Elaborado no encontrado: {idProducto}");

        if (elaborado.Producible)
        {
            if (elaborado.Receta is not null)
            {
                if (elaborado.Stock_actual < cantidad)
                    throw new InventarioException(
                        $"Stock insuficiente para {elaborado.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, solicitado: {cantidad}");

                var movimiento = elaborado.Canje(cantidad, referencia, 0.00M);
                await _unitWork.movimientos.Crear(movimiento);
            }

            return;
        }

        await ProcesarNoProducibleCanje(elaborado, cantidad, referencia);
    }

    /// <summary>Mismo criterio que pedidos sobre elaborados no producibles, sin opciones.</summary>
    private async Task ProcesarNoProducibleCanje(Elaborado elaborado, int cantidad, string referencia)
    {
        if (elaborado.Receta is null)
            return;

        var costo = 0.00M;

        foreach (var detalleReceta in elaborado.Receta.Detalles)
        {
            var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
            var cantidadFinal = cantidadPorPorcion * cantidad * (1 + detalleReceta.Merma / 100);

            if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                throw new InventarioException(
                    $"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, solicitado: {(int)cantidadFinal}");

            var factor = detalleReceta.Insumo.Factor_conversion > 0 ? detalleReceta.Insumo.Factor_conversion : 1;
            costo += (cantidadFinal * detalleReceta.Insumo.Costo) / factor;

            var movInsumo = detalleReceta.Insumo.AjusteVenta(referencia, (int)cantidadFinal);
            await _unitWork.Insumomovientos.Crear(movInsumo);
        }

        var movElaborado = elaborado.Canje(cantidad, referencia, costo);
        await _unitWork.movimientos.Crear(movElaborado);
    }

    /// <summary>Paralelo a combos en pedidos, con salidas marcadas como canje donde antes era venta.</summary>
    private async Task ProcesarComboCanje(int idProducto, int cantidad, string referencia)
    {
        var combo = await _unitWork.Combo.TraerPromocionCompleta(idProducto);

        if (combo is null)
            throw new InventarioException($"Combo no encontrado: {idProducto}");

        if (combo.Producto is null)
            throw new InventarioException($"Producto del combo no encontrado: {idProducto}");

        foreach (var detalle in combo.Detalles)
        {
            if (detalle.Producto is null)
                throw new InventarioException($"Producto no encontrado en combo {idProducto}");

            var cantidadTotal = detalle.Cantidad * cantidad;
            var referenciaCombo = $"{referencia}-{detalle.Producto.Nombre}";

            switch (detalle.Producto.Tipo)
            {
                case TiposProductos.Comprado:
                    var comprado = detalle.Producto.Comprado;
                    if (comprado is null)
                        throw new InventarioException($"Producto comprado no encontrado en combo: {detalle.Id_Producto}");

                    if (comprado.Stock_actual < cantidadTotal)
                        throw new InventarioException(
                            $"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {comprado.Stock_actual}, solicitado: {cantidadTotal}");

                    var movComprado = comprado.Canje(cantidadTotal, referenciaCombo);
                    await _unitWork.movimientos.Crear(movComprado);
                    break;

                case TiposProductos.Elaborado:
                    var elaborado = detalle.Producto.Elaborado;
                    if (elaborado is null)
                        throw new InventarioException($"Elaborado no encontrado en combo: {detalle.Id_Producto}");

                    if (elaborado.Producible)
                    {
                        if (elaborado.Receta is not null)
                        {
                            if (elaborado.Stock_actual < cantidadTotal)
                                throw new InventarioException(
                                    $"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, solicitado: {cantidadTotal}");

                            var movElabProd = elaborado.Canje(cantidadTotal, referenciaCombo, 0.00M);
                            await _unitWork.movimientos.Crear(movElabProd);
                        }
                    }
                    else
                    {
                        if (elaborado.Receta is not null)
                        {
                            foreach (var detalleReceta in elaborado.Receta.Detalles)
                            {
                                var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
                                var cantidadFinal =
                                    cantidadPorPorcion * cantidadTotal * (1 + detalleReceta.Merma / 100);

                                if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                                    throw new InventarioException(
                                        $"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, solicitado: {(int)cantidadFinal}");

                                var movInsumo = detalleReceta.Insumo.AjusteVenta(referenciaCombo, (int)cantidadFinal);
                                await _unitWork.Insumomovientos.Crear(movInsumo);
                            }
                        }
                    }

                    break;
            }
        }

        var movCombo = combo.Canje(cantidad, referencia);
        await _unitWork.movimientos.Crear(movCombo);
    }
}
