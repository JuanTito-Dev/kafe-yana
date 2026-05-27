using KafeYana.Application.Dtos.HitoCompraDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Servicios.HitosCompra;

public sealed class HitoCompraReclamoService(
    IUnitWork _unitWork,
    IInventarioCanjeService _inventarioCanje) : IHitoCompraReclamoService
{
    private const int CantidadReclamo = 1;

    public async Task<DtoHitosReclamadosCliente> ObtenerHitosReclamadosAsync(int idCliente)
    {
        if (await _unitWork.clientes.GetCliente(idCliente) is null)
            throw new InventarioException("Cliente no encontrado.");

        var historial = await _unitWork.historialHitoCompras.ObtenerReclamadosPorClienteAsync(idCliente);

        var reclamados = historial
            .Select(MapReclamado)
            .ToList();

        return new DtoHitosReclamadosCliente
        {
            Id_Cliente  = idCliente,
            Reclamados  = reclamados
        };
    }

    public async Task<ResultadoReclamoHitoCompra> ReclamarAsync(DtoReclamarHitoCompra dto)
    {
        var cliente = await _unitWork.clientes.GetCliente(dto.IdCliente);

        if (cliente is null)
            throw new InventarioException("Cliente no encontrado.");

        if (await _unitWork.historialHitoCompras.ExisteReclamoAsync(dto.IdCliente, dto.IdHitoCompra))
            throw new InventarioException("El cliente ya reclamó este hito por compras.");

        var hito = await _unitWork.hitosCompra.ObtenerActivoParaReclamoAsync(dto.IdHitoCompra);

        if (hito is null)
            throw new InventarioException("Hito por compra no encontrado o inactivo.");

        if (cliente.NumeroCompras < hito.NumeroCompras)
            throw new InventarioException(
                $"El cliente aún no alcanza este hito. Requiere {hito.NumeroCompras} compras y tiene {cliente.NumeroCompras}.");

        if (hito.ProductoCanjeable is null)
            throw new InventarioException("El hito no tiene producto canjeable configurado.");

        if (!hito.ProductoCanjeable.Activo)
            throw new InventarioException("El producto canjeable del hito está inactivo.");

        var codigoReclamo = $"RECLAMO-HITO-{hito.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var referencia    = $"HITO-COMPRA-{hito.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _inventarioCanje.DescontarInventarioAsync(
            hito.ProductoCanjeable.Id_Producto,
            CantidadReclamo,
            referencia);

        await _unitWork.historialHitoCompras.Crear(new HistorialHitoCompra
        {
            Id_Cliente               = dto.IdCliente,
            Id_HitoCompra            = hito.Id,
            NumeroComprasAlReclamar  = cliente.NumeroCompras,
            CodigoReclamo            = codigoReclamo,
            Fecha                    = DateTime.UtcNow
        });

        await _unitWork.SaveUnitWork();

        return new ResultadoReclamoHitoCompra
        {
            Mensaje                 = $"Hito de {hito.NumeroCompras} compras reclamado: \"{hito.ProductoCanjeable.NombreProducto}\".",
            IdHitoCompra            = hito.Id,
            NumeroComprasRequerido  = hito.NumeroCompras,
            NumeroComprasCliente    = cliente.NumeroCompras,
            CodigoReclamo           = codigoReclamo,
            IdProductoCanjeable     = hito.ProductoCanjeable.Id,
            NombreProducto          = hito.ProductoCanjeable.NombreProducto,
            Categoria               = hito.ProductoCanjeable.Categoria
        };
    }

    private static DtoHitoReclamadoItem MapReclamado(HistorialHitoCompra registro)
    {
        var hito = registro.HitoCompra!;
        var pc   = hito.ProductoCanjeable!;

        return new DtoHitoReclamadoItem
        {
            IdHitoCompra            = registro.Id_HitoCompra,
            NumeroComprasRequerido  = hito.NumeroCompras,
            NumeroComprasAlReclamar = registro.NumeroComprasAlReclamar,
            CodigoReclamo           = registro.CodigoReclamo,
            Fecha                   = registro.Fecha,
            Descripcion             = hito.Descripcion,
            Icono                   = hito.Icono,
            IdProductoCanjeable     = pc.Id,
            NombreProducto          = pc.NombreProducto,
            Categoria               = pc.Categoria
        };
    }
}
