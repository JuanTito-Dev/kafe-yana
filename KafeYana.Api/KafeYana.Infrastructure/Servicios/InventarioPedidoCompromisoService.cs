using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Dtos.InventarioPedido;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios;

public sealed class InventarioPedidoCompromisoService(IUnitWork _unitWork) : IInventarioPedidoCompromisoService
{
    public PedidoInventarioComprometido CrearCompromiso(
        int idPedido,
        string referencia,
        List<CompromisoLineaCalculo> lineas) =>
        new()
        {
            Id_Pedido = idPedido,
            Referencia = referencia,
            FechaCreacion = DateTime.UtcNow,
            Lineas = lineas.Select(l => new PedidoInventarioComprometidoLinea
            {
                TipoEntidad = l.TipoEntidad,
                Id_Producto = l.Id_Producto,
                Id_Insumo = l.Id_Insumo,
                Cantidad = l.Cantidad,
                Costo = l.Costo,
                Referencia = l.Referencia
            }).ToList()
        };

    public async Task RevertirCompromisoAsync(PedidoInventarioComprometido compromiso)
    {
        foreach (var linea in compromiso.Lineas)
            await DevolverLineaAsync(linea);

        _unitWork.pedidoInventarioCompromisos.Eliminar(compromiso);
    }

    public async Task RevertirCompromisoPorDetalleAsync(int idDetalleRonda)
    {
        var compromiso = await _unitWork.pedidoInventarioCompromisos.ObtenerPorDetalleAsync(idDetalleRonda);

        if (compromiso is null)
            return;

        await RevertirCompromisoAsync(compromiso);
    }

    public async Task RevertirCompromisoParcialAsync(int idDetalleRonda, int cantidadALiberar, int cantidadOriginalDetalle)
    {
        if (cantidadALiberar <= 0 || cantidadOriginalDetalle <= 0)
            return;

        var compromiso = await _unitWork.pedidoInventarioCompromisos.ObtenerPorDetalleAsync(idDetalleRonda);
        if (compromiso is null)
            return;

        if (cantidadALiberar >= cantidadOriginalDetalle)
        {
            // Se libera todo lo comprometido para este detalle.
            await RevertirCompromisoAsync(compromiso);
            return;
        }

        // Proporción de cada línea (comprometida para cantidadOriginalDetalle
        // unidades del producto) que corresponde a la porción que se libera.
        // Nota: si linea.Cantidad no es múltiplo exacto de cantidadOriginalDetalle
        // el redondeo hacia abajo puede dejar una fracción mínima sin liberar;
        // aceptable frente a la alternativa de sobre-liberar stock.
        foreach (var linea in compromiso.Lineas)
        {
            var cantidadALiberarLinea = linea.Cantidad * cantidadALiberar / cantidadOriginalDetalle;
            if (cantidadALiberarLinea <= 0) continue;

            await DevolverLineaAsync(linea, cantidadALiberarLinea);
            linea.Cantidad -= cantidadALiberarLinea;
        }
    }

    public async Task AplicarMovimientosYCerrarAsync(int idPedido, string codigoVenta)
    {
        var compromisos = await _unitWork.pedidoInventarioCompromisos.ObtenerPorPedidoAsync(idPedido);

        foreach (var compromiso in compromisos)
        {
            foreach (var linea in compromiso.Lineas)
                await CrearMovimientoLineaAsync(linea, codigoVenta);
        }

        _unitWork.pedidoInventarioCompromisos.EliminarRango(compromisos);
    }

    private async Task DevolverLineaAsync(PedidoInventarioComprometidoLinea linea) =>
        await DevolverLineaAsync(linea, linea.Cantidad);

    private async Task DevolverLineaAsync(PedidoInventarioComprometidoLinea linea, int cantidad)
    {
        if (cantidad <= 0) return;

        switch (linea.TipoEntidad)
        {
            case TiposCompromisoInventario.Comprado:
                await DevolverCompradoAsync(linea, cantidad);
                break;

            case TiposCompromisoInventario.Elaborado:
                await DevolverElaboradoAsync(linea, cantidad);
                break;

            case TiposCompromisoInventario.Insumo:
                await DevolverInsumoAsync(linea, cantidad);
                break;

            case TiposCompromisoInventario.Promocion:
                break;
        }
    }

    private async Task CrearMovimientoLineaAsync(PedidoInventarioComprometidoLinea linea, string codigoVenta)
    {
        var referencia = string.IsNullOrWhiteSpace(linea.Referencia) ? codigoVenta : linea.Referencia;

        switch (linea.TipoEntidad)
        {
            case TiposCompromisoInventario.Comprado:
            {
                var producto = await _unitWork.productos.TraerProducto(linea.Id_Producto!.Value, comprado: true);
                if (producto?.Comprado is null)
                    throw new InventarioException($"Producto comprado no encontrado: {linea.Id_Producto}");

                var movimiento = producto.Comprado.CrearMovimientoVenta(linea.Cantidad, codigoVenta);
                await _unitWork.movimientos.Crear(movimiento);
                break;
            }

            case TiposCompromisoInventario.Elaborado:
            {
                var elaborado = await _unitWork.elaborados.TraerElaborado(linea.Id_Producto!.Value, withreceta: false);
                if (elaborado is null)
                    throw new InventarioException($"Elaborado no encontrado: {linea.Id_Producto}");

                var movimiento = elaborado.CrearMovimientoVenta(linea.Cantidad, codigoVenta, linea.Costo);
                await _unitWork.movimientos.Crear(movimiento);
                break;
            }

            case TiposCompromisoInventario.Insumo:
            {
                var insumo = await _unitWork.insumos.FindByIdAsync(linea.Id_Insumo!.Value);
                if (insumo is null)
                    throw new InventarioException($"Insumo no encontrado: {linea.Id_Insumo}");

                var movimiento = insumo.CrearMovimientoVenta(codigoVenta, linea.Cantidad);
                await _unitWork.Insumomovientos.Crear(movimiento);
                break;
            }

            case TiposCompromisoInventario.Promocion:
            {
                var combo = await _unitWork.Combo.TraerPromocionCompleta(linea.Id_Producto!.Value);
                if (combo is null)
                    throw new InventarioException($"Combo no encontrado: {linea.Id_Producto}");

                var movimiento = combo.CrearMovimientoVenta(linea.Cantidad, codigoVenta);
                await _unitWork.movimientos.Crear(movimiento);
                break;
            }

            default:
                throw new InventarioException($"Tipo de compromiso desconocido: {linea.TipoEntidad}");
        }
    }

    private async Task DevolverCompradoAsync(PedidoInventarioComprometidoLinea linea, int cantidad)
    {
        var producto = await _unitWork.productos.TraerProducto(linea.Id_Producto!.Value, comprado: true);
        if (producto?.Comprado is null)
            throw new InventarioException($"Producto comprado no encontrado: {linea.Id_Producto}");

        producto.Comprado.DevolverStock(cantidad);
    }

    private async Task DevolverElaboradoAsync(PedidoInventarioComprometidoLinea linea, int cantidad)
    {
        var elaborado = await _unitWork.elaborados.TraerElaborado(linea.Id_Producto!.Value, withreceta: false);
        if (elaborado is null)
            throw new InventarioException($"Elaborado no encontrado: {linea.Id_Producto}");

        elaborado.DevolverStock(cantidad);
    }

    private async Task DevolverInsumoAsync(PedidoInventarioComprometidoLinea linea, int cantidad)
    {
        var insumo = await _unitWork.insumos.FindByIdAsync(linea.Id_Insumo!.Value);
        if (insumo is null)
            throw new InventarioException($"Insumo no encontrado: {linea.Id_Insumo}");

        insumo.DevolverStock(cantidad);
    }
}
