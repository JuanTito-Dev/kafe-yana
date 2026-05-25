using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;

/// <summary>Aplica recompensas de promoción permanente. Implementar Descuento y ProductoGratis aquí.</summary>
internal interface IRecompensaPromocionPermanenteAplicador
{
    string TipoRecompensa { get; }

    Task<ResultadoAplicacionPromocionPermanente> AplicarAsync(
        Cliente cliente,
        PromocionPermanente promocion,
        string codigoVenta,
        IUnitWork unitWork);
}

internal sealed class PuntosExtraPromocionPermanenteAplicador : IRecompensaPromocionPermanenteAplicador
{
    public string TipoRecompensa => TipoRecompensaPromocion.PuntosExtra;

    public async Task<ResultadoAplicacionPromocionPermanente> AplicarAsync(
        Cliente cliente,
        PromocionPermanente promocion,
        string codigoVenta,
        IUnitWork unitWork)
    {
        cliente.AgregarPuntos(promocion.ValorRecompensa);

        await unitWork.historialPromocionPermanentes.Crear(new HistorialPromocionPermanente
        {
            Id_Cliente              = cliente.Id,
            Id_PromocionPermanente  = promocion.Id,
            CodigoVenta             = codigoVenta,
            TipoRecompensa          = promocion.TipoRecompensa,
            ValorRecompensa         = promocion.ValorRecompensa,
            TipoCondicion           = promocion.TipoCondicion,
            ValorCondicion          = promocion.ValorCondicion,
            Fecha                   = DateTime.UtcNow
        });

        return new ResultadoAplicacionPromocionPermanente
        {
            NombrePromocion = promocion.Nombre,
            PuntosExtra     = promocion.ValorRecompensa,
            Mensaje         = $"Se agregaron {promocion.ValorRecompensa} puntos extra por la promoción \"{promocion.Nombre}\"."
        };
    }
}
