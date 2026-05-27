using KafeYana.Application.Dtos.ProductoCanjeable;

using KafeYana.Application.Exceptions;

using KafeYana.Application.IRepositorio;

using KafeYana.Application.IServicios;



namespace KafeYana.Infrastructure.Servicios;



/// <summary>Canje por puntos — misma regla de inventario que pedidos (<c>Detalle_RondaService</c>),

/// con movimiento <c>Canje</c> en productos/promo.</summary>

public class CanjeProductoService(IUnitWork _unitWork, IInventarioCanjeService _inventarioCanje) : ICanjeProductoService

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



        var referencia = $"CANJE-{pc.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";



        await _inventarioCanje.DescontarInventarioAsync(pc.Id_Producto, CantidadCanjeUnitaria, referencia);



        cliente.DescontarPuntosPorCanje(pc.Puntos);

        await _unitWork.SaveUnitWork();

    }

}

