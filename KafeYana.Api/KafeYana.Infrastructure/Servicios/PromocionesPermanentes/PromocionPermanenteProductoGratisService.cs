using KafeYana.Application.Dtos.ProductoCanjeable;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;

public sealed class PromocionPermanenteProductoGratisService(
    IUnitWork _unitWork,
    IInventarioCanjeService _inventarioCanje) : IPromocionPermanenteProductoGratisService
{
    private const int CantidadReclamo = 1;

    public async Task<DtoPromocionesGratisCliente> ObtenerPromocionesGratisClienteAsync(int idCliente)
    {
        if (await _unitWork.clientes.GetCliente(idCliente) is null)
            throw new InventarioException("Cliente no encontrado.");

        var promociones = await _unitWork.promocionPermanentes
            .ObtenerActivasPorRecompensaAsync(TipoRecompensaPromocion.ProductoGratis);

        if (promociones.Count == 0)
        {
            return new DtoPromocionesGratisCliente { Id_Cliente = idCliente };
        }

        var idsPromo = promociones.Select(p => p.Id).ToList();
        var progresoPorPromo = await _unitWork.promocionPermanenteProgresos
            .ObtenerPorClienteYPromocionesAsync(idCliente, idsPromo);

        var disponibles = new List<DtoPromocionGratisItem>();
        var enProgreso = new List<DtoPromocionGratisItem>();

        foreach (var promo in promociones)
        {
            if (!EsPromoValidaParaConsulta(promo))
                continue;

            progresoPorPromo.TryGetValue(promo.Id, out var progreso);

            if (PromocionPermanenteEvaluador.EsProductoGratisReclamable(promo, progreso))
                disponibles.Add(MapItem(promo, progreso));
            else if (PromocionPermanenteEvaluador.EsProductoGratisEnProgreso(promo, progreso))
                enProgreso.Add(MapItem(promo, progreso));
        }

        return new DtoPromocionesGratisCliente
        {
            Id_Cliente  = idCliente,
            Disponibles = disponibles,
            EnProgreso  = enProgreso
        };
    }

    public async Task<ResultadoReclamoPromocionGratis> ReclamarAsync(DtoReclamarPromocionGratis dto)
    {
        if (await _unitWork.clientes.GetCliente(dto.IdCliente) is null)
            throw new InventarioException("Cliente no encontrado.");

        var promo = await _unitWork.promocionPermanentes.ObtenerActivaProductoGratisAsync(dto.IdPromocionPermanente);

        if (promo is null)
            throw new InventarioException("Promoción permanente no encontrada o inactiva.");

        if (promo.Id_ProductoCanjeable is null || promo.ProductoCanjeable is null)
            throw new InventarioException("La promoción no tiene producto canjeable configurado.");

        if (!promo.ProductoCanjeable.Activo)
            throw new InventarioException("El producto canjeable de la promoción está inactivo.");

        var progreso = await ObtenerOCrearProgresoAsync(dto.IdCliente, promo.Id);

        if (!PromocionPermanenteEvaluador.EsProductoGratisReclamable(promo, progreso))
            throw new InventarioException("El cliente no cumple la condición para reclamar esta promoción.");

        var referencia = $"PROMO-GRATIS-{promo.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _inventarioCanje.DescontarInventarioAsync(
            promo.ProductoCanjeable.Id_Producto,
            CantidadReclamo,
            referencia);

        PromocionPermanenteEvaluador.MarcarProductoGratisReclamado(promo, progreso);

        var codigoReclamo = $"RECLAMO-GRATIS-{promo.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _unitWork.historialPromocionPermanentes.Crear(new HistorialPromocionPermanente
        {
            Id_Cliente             = dto.IdCliente,
            Id_PromocionPermanente = promo.Id,
            CodigoVenta            = codigoReclamo,
            TipoRecompensa         = promo.TipoRecompensa,
            ValorRecompensa        = 0,
            TipoCondicion          = promo.TipoCondicion,
            ValorCondicion         = promo.ValorCondicion,
            Fecha                  = DateTime.UtcNow
        });

        await _unitWork.SaveUnitWork();

        return new ResultadoReclamoPromocionGratis
        {
            Mensaje               = $"Producto gratis reclamado: \"{promo.ProductoCanjeable.NombreProducto}\" por la promoción \"{promo.Nombre}\".",
            IdPromocionPermanente = promo.Id,
            NombrePromocion       = promo.Nombre,
            IdProductoCanjeable   = promo.ProductoCanjeable.Id,
            NombreProducto        = promo.ProductoCanjeable.NombreProducto,
            Categoria             = promo.ProductoCanjeable.Categoria
        };
    }

    public async Task RegistrarProgresoPostVentaAsync(int idCliente, decimal subtotalVenta)
    {
        var promociones = await _unitWork.promocionPermanentes
            .ObtenerActivasPorRecompensaAsync(TipoRecompensaPromocion.ProductoGratis);

        if (promociones.Count == 0)
            return;

        var idsPromo = promociones.Select(p => p.Id).ToList();
        var progresoPorPromo = await _unitWork.promocionPermanenteProgresos
            .ObtenerPorClienteYPromocionesAsync(idCliente, idsPromo);

        foreach (var promo in promociones)
        {
            if (PromocionPermanenteCondicionEvaluador.DebeOmitir(promo.TipoCondicion))
                continue;

            PromocionPermanenteEvaluador.RegistrarProgresoProductoGratisPostVenta(
                idCliente,
                promo,
                subtotalVenta,
                progresoPorPromo);
        }

        await PromocionPermanenteEvaluador.PersistirProgresosNuevosAsync(_unitWork, progresoPorPromo);
    }

    private static bool EsPromoValidaParaConsulta(PromocionPermanente promo)
    {
        if (PromocionPermanenteCondicionEvaluador.DebeOmitir(promo.TipoCondicion))
            return false;

        if (promo.Id_ProductoCanjeable is null || promo.ProductoCanjeable is null)
            return false;

        return promo.ProductoCanjeable.Activo;
    }

    private async Task<PromocionPermanenteProgreso> ObtenerOCrearProgresoAsync(int idCliente, int idPromo)
    {
        var progresoPorPromo = await _unitWork.promocionPermanenteProgresos
            .ObtenerPorClienteYPromocionesAsync(idCliente, [idPromo]);

        if (progresoPorPromo.TryGetValue(idPromo, out var progreso))
            return progreso;

        progreso = new PromocionPermanenteProgreso
        {
            Id_Cliente             = idCliente,
            Id_PromocionPermanente = idPromo,
            ContadorCompras        = 0
        };

        await _unitWork.promocionPermanenteProgresos.Crear(progreso);
        return progreso;
    }

    private static DtoPromocionGratisItem MapItem(PromocionPermanente promo, PromocionPermanenteProgreso? progreso)
    {
        var pc = promo.ProductoCanjeable!;

        return new DtoPromocionGratisItem
        {
            IdPromocionPermanente = promo.Id,
            NombrePromocion       = promo.Nombre,
            TipoCondicion         = promo.TipoCondicion,
            ValorCondicion        = promo.ValorCondicion,
            ProgresoActual        = promo.TipoCondicion == TipoCondicionPromocion.NCompras
                ? progreso?.ContadorCompras ?? 0
                : null,
            IdProductoCanjeable   = pc.Id,
            NombreProducto        = pc.NombreProducto,
            Categoria             = pc.Categoria
        };
    }
}
