using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;

internal sealed record CandidataPromocionPermanente(
    PromocionPermanente Promo,
    PromocionPermanenteProgreso? Progreso);

/// <summary>Evaluación compartida de condiciones (PuntosExtra, Descuento, ProductoGratis).</summary>
internal static class PromocionPermanenteEvaluador
{
    public static decimal CalcularMontoDescuento(decimal subtotal, int porcentaje)
        => Math.Round(subtotal * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);

    public static async Task<IReadOnlyList<CandidataPromocionPermanente>> EvaluarCandidatasAsync(
        IUnitWork unitWork,
        int idCliente,
        decimal totalPedido,
        string tipoRecompensa,
        bool persistirProgresoNCompras)
    {
        var promociones = await unitWork.promocionPermanentes.ObtenerActivasPorRecompensaAsync(tipoRecompensa);
        if (promociones.Count == 0)
            return [];

        var idsPromo = promociones.Select(p => p.Id).ToList();
        var progresoPorPromo = await unitWork.promocionPermanenteProgresos
            .ObtenerPorClienteYPromocionesAsync(idCliente, idsPromo);

        var candidatas = new List<CandidataPromocionPermanente>();

        foreach (var promo in promociones)
        {
            if (PromocionPermanenteCondicionEvaluador.DebeOmitir(promo.TipoCondicion))
                continue;

            switch (promo.TipoCondicion)
            {
                case TipoCondicionPromocion.NCompras:
                    if (EvaluarNCompras(idCliente, promo, progresoPorPromo, persistirProgresoNCompras, out var progresoN))
                        candidatas.Add(new CandidataPromocionPermanente(promo, progresoN));
                    break;

                case TipoCondicionPromocion.MontoMinimo:
                    if (PromocionPermanenteCondicionEvaluador.CumpleMontoMinimo(totalPedido, promo.ValorCondicion))
                        candidatas.Add(new CandidataPromocionPermanente(promo, null));
                    break;
            }
        }

        if (persistirProgresoNCompras)
            await PersistirProgresosNuevosAsync(unitWork, progresoPorPromo);

        return candidatas;
    }

    public static CandidataPromocionPermanente? SeleccionarMejorPuntosExtra(
        IReadOnlyList<CandidataPromocionPermanente> candidatas)
        => candidatas
            .OrderByDescending(c => c.Promo.ValorRecompensa)
            .ThenBy(c => c.Promo.Id)
            .FirstOrDefault();

    public static (CandidataPromocionPermanente Candidata, decimal MontoDescuento)? SeleccionarMejorDescuento(
        IReadOnlyList<CandidataPromocionPermanente> candidatas,
        decimal subtotal)
    {
        if (candidatas.Count == 0)
            return null;

        var mejor = candidatas
            .Select(c => (Candidata: c, Monto: CalcularMontoDescuento(subtotal, c.Promo.ValorRecompensa)))
            .OrderByDescending(x => x.Monto)
            .ThenByDescending(x => x.Candidata.Promo.ValorRecompensa)
            .ThenBy(x => x.Candidata.Promo.Id)
            .First();

        return (mejor.Candidata, mejor.Monto);
    }

    public static void ReiniciarContadorNCompras(CandidataPromocionPermanente candidata)
    {
        if (candidata.Progreso is not null
            && candidata.Promo.TipoCondicion == TipoCondicionPromocion.NCompras)
        {
            candidata.Progreso.ContadorCompras = 0;
        }
    }

    public static bool EsProductoGratisReclamable(PromocionPermanente promo, PromocionPermanenteProgreso? progreso)
    {
        return promo.TipoCondicion switch
        {
            TipoCondicionPromocion.NCompras => (progreso?.ContadorCompras ?? 0) >= promo.ValorCondicion,
            TipoCondicionPromocion.MontoMinimo => progreso?.ReclamoMontoMinimoPendiente == true,
            _ => false
        };
    }

    public static bool EsProductoGratisEnProgreso(PromocionPermanente promo, PromocionPermanenteProgreso? progreso)
    {
        if (promo.TipoCondicion != TipoCondicionPromocion.NCompras)
            return false;

        var contador = progreso?.ContadorCompras ?? 0;
        return contador < promo.ValorCondicion;
    }

    public static void RegistrarProgresoProductoGratisPostVenta(
        int idCliente,
        PromocionPermanente promo,
        decimal subtotalVenta,
        Dictionary<int, PromocionPermanenteProgreso> progresoPorPromo)
    {
        if (!progresoPorPromo.TryGetValue(promo.Id, out var progreso))
        {
            progreso = new PromocionPermanenteProgreso
            {
                Id_Cliente             = idCliente,
                Id_PromocionPermanente = promo.Id,
                ContadorCompras        = 0
            };
            progresoPorPromo[promo.Id] = progreso;
        }

        switch (promo.TipoCondicion)
        {
            case TipoCondicionPromocion.NCompras:
                progreso.ContadorCompras++;
                break;

            case TipoCondicionPromocion.MontoMinimo:
                if (PromocionPermanenteCondicionEvaluador.CumpleMontoMinimo(subtotalVenta, promo.ValorCondicion))
                    progreso.ReclamoMontoMinimoPendiente = true;
                break;
        }
    }

    public static void MarcarProductoGratisReclamado(PromocionPermanente promo, PromocionPermanenteProgreso progreso)
    {
        switch (promo.TipoCondicion)
        {
            case TipoCondicionPromocion.NCompras:
                progreso.ContadorCompras = 0;
                break;

            case TipoCondicionPromocion.MontoMinimo:
                progreso.ReclamoMontoMinimoPendiente = false;
                break;
        }
    }

    private static bool EvaluarNCompras(
        int idCliente,
        PromocionPermanente promo,
        Dictionary<int, PromocionPermanenteProgreso> progresoPorPromo,
        bool persistir,
        out PromocionPermanenteProgreso progreso)
    {
        if (!progresoPorPromo.TryGetValue(promo.Id, out progreso!))
        {
            progreso = new PromocionPermanenteProgreso
            {
                Id_Cliente             = idCliente,
                Id_PromocionPermanente = promo.Id,
                ContadorCompras        = 0
            };
        }

        var contadorSimulado = progreso.ContadorCompras + 1;
        var califica = contadorSimulado >= promo.ValorCondicion;

        if (persistir)
        {
            progreso.ContadorCompras = contadorSimulado;
            progresoPorPromo[promo.Id] = progreso;
        }

        return califica;
    }

    public static async Task PersistirProgresosNuevosAsync(
        IUnitWork unitWork,
        Dictionary<int, PromocionPermanenteProgreso> progresoPorPromo)
    {
        foreach (var progreso in progresoPorPromo.Values)
        {
            if (progreso.Id == 0)
                await unitWork.promocionPermanenteProgresos.Crear(progreso);
        }
    }
}
