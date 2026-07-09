using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Infrastructure.Servicios;

public sealed class RondaPedidoService(
    IUnitWork _unitWork,
    Detalle_RondaService _detalleRondaService,
    IInventarioPedidoCompromisoService _compromisoService) : IRondaPedidoService
{
    public async Task<Ronda> EditarRondaAsync(int idRonda, DtoRondaEditar datos)
    {
        var ronda = await _unitWork.rondas.TraerConDetallesAsync(idRonda);

        if (ronda is null)
            throw new DetalleRondaException($"Ronda no encontrada: {idRonda}");

        if (ronda.Id_Pedido != datos.Id_Pedido)
            throw new DetalleRondaException("El pedido no corresponde a la ronda.");

        if (datos.Detalles.Count == 0)
            throw new DetalleRondaException("La ronda debe tener al menos un detalle.");

        var referencia = Detalle_RondaService.GenerarReferenciaEdicion(datos.Id_Pedido, idRonda);
        var idsEnPayload = datos.Detalles
            .Where(d => d.Id_Detalle.HasValue)
            .Select(d => d.Id_Detalle!.Value)
            .ToHashSet();

        foreach (var detalleExistente in ronda.Detalle.ToList())
        {
            if (!idsEnPayload.Contains(detalleExistente.Id))
            {
                await EliminarDetalleInternoConPisoAsync(detalleExistente);
                ronda.Detalle.Remove(detalleExistente);
            }
        }

        foreach (var item in datos.Detalles)
        {
            if (item.Id_Detalle.HasValue)
            {
                var existente = ronda.Detalle.FirstOrDefault(d => d.Id == item.Id_Detalle.Value);
                if (existente is null)
                    throw new DetalleRondaException($"Detalle {item.Id_Detalle} no pertenece a la ronda.");

                if (item.Cantidad < existente.CantidadDescontada)
                    existente.CantidadDescontada = item.Cantidad;

                await ReemplazarDetalleAsync(datos.Id_Pedido, existente, item, referencia);
            }
            else
            {
                var dto = new DtoRondadetalle
                {
                    Id_Producto = item.Id_Producto,
                    Cantidad = item.Cantidad,
                    Ids_Opcion = item.Ids_Opcion,
                    Nota = item.Nota
                };

                var nuevo = await _detalleRondaService.ProcesarDetalleAsync(datos.Id_Pedido, dto, referencia);
                nuevo.Id_Ronda = idRonda;
                ronda.Detalle.Add(nuevo);
            }
        }

        ronda.SubTotal = CalcularSubTotalRonda(ronda.Detalle);

        await RecalcularTotalPedidoAsync(datos.Id_Pedido);

        return ronda;
    }

    public async Task EliminarRondaAsync(int idRonda, int idPedido)
    {
        var ronda = await _unitWork.rondas.TraerConDetallesAsync(idRonda);

        if (ronda is null)
            throw new DetalleRondaException($"Ronda no encontrada: {idRonda}");

        if (ronda.Id_Pedido != idPedido)
            throw new DetalleRondaException("El pedido no corresponde a la ronda.");

        foreach (var detalle in ronda.Detalle.ToList())
        {
            await EliminarDetalleInternoConPisoAsync(detalle);
            ronda.Detalle.Remove(detalle);
        }

        await _unitWork.rondas.Remove(ronda);

        await RecalcularTotalPedidoAsync(idPedido);
    }

    public async Task<Detalle_ronda> EditarDetalleAsync(int idDetalle, DtoRondadetalle datos, int idPedido)
    {
        var detalle = await _unitWork.detallesRondas.TraerConRelacionesAsync(idDetalle);

        if (detalle?.ronda is null)
            throw new DetalleRondaException($"Detalle no encontrado: {idDetalle}");

        if (detalle.ronda.Id_Pedido != idPedido)
            throw new DetalleRondaException("El pedido no corresponde al detalle.");

        if (datos.Cantidad < detalle.CantidadDescontada)
            detalle.CantidadDescontada = datos.Cantidad;

        var referencia = Detalle_RondaService.GenerarReferenciaEdicion(idPedido, detalle.Id_Ronda);
        var actualizado = await ReemplazarDetalleAsync(idPedido, detalle, new DtoRondadetalleEditar
        {
            Id_Detalle = idDetalle,
            Id_Producto = datos.Id_Producto,
            Cantidad = datos.Cantidad,
            Ids_Opcion = datos.Ids_Opcion,
            Nota = datos.Nota
        }, referencia);

        detalle.ronda.SubTotal = CalcularSubTotalRonda(detalle.ronda.Detalle);
        await RecalcularTotalPedidoAsync(idPedido);

        return actualizado;
    }

    public async Task EliminarDetalleAsync(int idDetalle, int idPedido)
    {
        var detalle = await _unitWork.detallesRondas.TraerConRelacionesAsync(idDetalle);

        if (detalle?.ronda is null)
            throw new DetalleRondaException($"Detalle no encontrado: {idDetalle}");

        if (detalle.ronda.Id_Pedido != idPedido)
            throw new DetalleRondaException("El pedido no corresponde al detalle.");

        var ronda = detalle.ronda;
        await EliminarDetalleInternoConPisoAsync(detalle);
        ronda.Detalle.Remove(detalle);

        ronda.SubTotal = CalcularSubTotalRonda(ronda.Detalle);
        await RecalcularTotalPedidoAsync(idPedido);
    }

    public async Task RecalcularTotalPedidoAsync(int idPedido)
    {
        var pedido = await _unitWork.Pedidos.FindByIdAsync(idPedido);

        if (pedido is null)
            throw new DetalleRondaException("Pedido no encontrado.");

        pedido.Total = await _unitWork.rondas.SumSubTotalPorPedidoAsync(idPedido);
    }

    private async Task<Detalle_ronda> ReemplazarDetalleAsync(
        int idPedido,
        Detalle_ronda detalleExistente,
        DtoRondadetalleEditar item,
        string referencia)
    {
        await RevertirDetalleAsync(detalleExistente);

        var dto = new DtoRondadetalle
        {
            Id_Producto = item.Id_Producto,
            Cantidad = item.Cantidad,
            Ids_Opcion = item.Ids_Opcion,
            Nota = item.Nota
        };

        var procesado = await _detalleRondaService.ProcesarDetalleAsync(idPedido, dto, referencia);

        detalleExistente.Id_Producto = procesado.Id_Producto;
        detalleExistente.Codigo = procesado.Codigo;
        detalleExistente.CodigoSin = procesado.CodigoSin;
        detalleExistente.CodigoUnidadMedida = procesado.CodigoUnidadMedida;
        detalleExistente.Nombre_Producto = procesado.Nombre_Producto;
        detalleExistente.Cantidad = procesado.Cantidad;
        detalleExistente.Precio = procesado.Precio;
        detalleExistente.Nota = procesado.Nota;
        detalleExistente.Ubicacion = procesado.Ubicacion;

        detalleExistente.Opciones.Clear();
        foreach (var opcion in procesado.Opciones)
            detalleExistente.Opciones.Add(opcion);

        detalleExistente.ItemsCombo.Clear();
        foreach (var comboItem in procesado.ItemsCombo)
            detalleExistente.ItemsCombo.Add(comboItem);

        detalleExistente.CompromisoInventario = procesado.CompromisoInventario;

        return detalleExistente;
    }

    /// <summary>
    /// Borra un detalle de ronda por completo, revirtiendo del compromiso de
    /// inventario SOLO la porción aún no vendida (<c>Cantidad - CantidadDescontada</c>):
    /// lo que ya se vendió vía sub-venta consumió stock real y no se devuelve.
    /// La sub-venta ya tiene su propia copia independiente de estos datos
    /// (<see cref="SubVentaDetalle"/>) y no se ve afectada por este borrado.
    /// </summary>
    private async Task EliminarDetalleInternoConPisoAsync(Detalle_ronda detalle)
    {
        if (detalle.CantidadDescontada == 0)
        {
            await RevertirDetalleAsync(detalle);
        }
        else
        {
            var cantidadALiberar = detalle.Cantidad - detalle.CantidadDescontada;
            if (cantidadALiberar > 0)
            {
                await _compromisoService.RevertirCompromisoParcialAsync(
                    detalle.Id, cantidadALiberar, detalle.Cantidad);
            }
        }

        await _unitWork.detallesRondas.Remove(detalle);
    }

    private async Task RevertirDetalleAsync(Detalle_ronda detalle)
    {
        if (detalle.CompromisoInventario is not null)
            await _compromisoService.RevertirCompromisoAsync(detalle.CompromisoInventario);
        else
            await _compromisoService.RevertirCompromisoPorDetalleAsync(detalle.Id);
    }

    private static decimal CalcularSubTotalRonda(IEnumerable<Detalle_ronda> detalles) =>
        detalles.Sum(d => d.Precio * d.Cantidad);
}
