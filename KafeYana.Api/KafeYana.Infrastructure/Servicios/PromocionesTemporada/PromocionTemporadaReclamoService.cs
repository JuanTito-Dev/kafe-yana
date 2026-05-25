using KafeYana.Application.Dtos.PromocionTemporadaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Servicios.PromocionesTemporada;

public sealed class PromocionTemporadaReclamoService(
    IUnitWork _unitWork,
    IInventarioCanjeService _inventarioCanje) : IPromocionTemporadaReclamoService
{
    private const int CantidadReclamo = 1;

    public async Task<DtoPromocionesTemporadaCliente> ObtenerProductosReclamablesAsync(int idCliente)
    {
        if (await _unitWork.clientes.GetCliente(idCliente) is null)
            throw new InventarioException("Cliente no encontrado.");

        var ahora = DateTime.UtcNow;
        var promociones = await _unitWork.promocionTemporadas.ObtenerActivasVigentesAsync(ahora);

        if (promociones.Count == 0)
            return new DtoPromocionesTemporadaCliente { Id_Cliente = idCliente };

        var reclamadas = await _unitWork.historialPromocionTemporadas
            .ObtenerIdsPromocionesReclamadasAsync(idCliente);

        var productos = new List<DtoPromocionTemporadaProductoItem>();

        foreach (var promo in promociones)
        {
            if (reclamadas.Contains(promo.Id))
                continue;

            foreach (var enlace in promo.ProductosCanjeables)
            {
                var pc = enlace.ProductoCanjeable;
                if (pc is null || !pc.Activo)
                    continue;

                productos.Add(new DtoPromocionTemporadaProductoItem
                {
                    IdPromocionTemporada = promo.Id,
                    NombrePromocion      = promo.Nombre,
                    FechaInicio          = promo.FechaInicio,
                    FechaFin             = promo.FechaFin,
                    IdProductoCanjeable  = pc.Id,
                    NombreProducto       = pc.NombreProducto,
                    Categoria            = pc.Categoria,
                    Puntos               = pc.Puntos
                });
            }
        }

        return new DtoPromocionesTemporadaCliente
        {
            Id_Cliente = idCliente,
            Productos  = productos
        };
    }

    public async Task<ResultadoReclamoPromocionTemporada> ReclamarAsync(DtoReclamarPromocionTemporada dto)
    {
        if (await _unitWork.clientes.GetCliente(dto.IdCliente) is null)
            throw new InventarioException("Cliente no encontrado.");

        if (await _unitWork.historialPromocionTemporadas.ExisteReclamoAsync(dto.IdCliente, dto.IdPromocionTemporada))
            throw new InventarioException("El cliente ya reclamó esta promoción por temporada.");

        var ahora = DateTime.UtcNow;
        var promo = await _unitWork.promocionTemporadas
            .ObtenerActivaVigenteParaReclamoAsync(dto.IdPromocionTemporada, ahora);

        if (promo is null)
            throw new InventarioException("Promoción por temporada no encontrada, inactiva o fuera de vigencia.");

        var productosActivos = promo.ProductosCanjeables
            .Select(x => x.ProductoCanjeable)
            .Where(pc => pc is not null && pc.Activo)
            .Cast<ProductoCanjeable>()
            .ToList();

        if (productosActivos.Count == 0)
            throw new InventarioException("La promoción no tiene productos canjeables activos para reclamar.");

        var codigoReclamo = $"RECLAMO-TEMPORADA-{promo.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var referenciaBase = $"PROMO-TEMPORADA-{promo.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var productosReclamados = new List<DtoPromocionTemporadaProductoReclamado>();

        foreach (var pc in productosActivos)
        {
            var referencia = $"{referenciaBase}-{pc.Id}";

            await _inventarioCanje.DescontarInventarioAsync(
                pc.Id_Producto,
                CantidadReclamo,
                referencia);

            productosReclamados.Add(new DtoPromocionTemporadaProductoReclamado
            {
                IdProductoCanjeable = pc.Id,
                NombreProducto      = pc.NombreProducto,
                Categoria           = pc.Categoria
            });
        }

        await _unitWork.historialPromocionTemporadas.Crear(new HistorialPromocionTemporada
        {
            Id_Cliente             = dto.IdCliente,
            Id_PromocionTemporada  = promo.Id,
            CodigoReclamo          = codigoReclamo,
            Fecha                  = DateTime.UtcNow
        });

        await _unitWork.SaveUnitWork();

        var nombres = string.Join(", ", productosReclamados.Select(p => $"\"{p.NombreProducto}\""));

        return new ResultadoReclamoPromocionTemporada
        {
            Mensaje              = $"Promoción por temporada \"{promo.Nombre}\" reclamada: {nombres}.",
            IdPromocionTemporada = promo.Id,
            NombrePromocion      = promo.Nombre,
            CodigoReclamo        = codigoReclamo,
            ProductosReclamados  = productosReclamados
        };
    }
}
