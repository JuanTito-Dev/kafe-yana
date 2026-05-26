using KafeYana.Domain.Entities;

namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;

/// <summary>Evalúa condiciones de promoción permanente (extensible a Requeridos).</summary>
internal static class PromocionPermanenteCondicionEvaluador
{
    public static bool CumpleMontoMinimo(decimal totalVenta, int valorCondicion)
        => totalVenta >= valorCondicion;

    public static bool DebeOmitir(string tipoCondicion)
        => tipoCondicion == Domain.TiposDeDatos.TipoCondicionPromocion.Requeridos;
}
