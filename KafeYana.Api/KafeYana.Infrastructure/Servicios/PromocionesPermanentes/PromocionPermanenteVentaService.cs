using KafeYana.Application.Dtos.VentaDtos;

using KafeYana.Application.IRepositorio;

using KafeYana.Application.IServicios;

using KafeYana.Domain.Entities;

using KafeYana.Domain.TiposDeDatos;



namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;



/// <summary>Orquesta promociones permanentes PuntosExtra al cerrar venta.</summary>

public sealed class PromocionPermanenteVentaService(IUnitWork _unitWork) : IPromocionPermanenteVentaService

{

    private readonly PuntosExtraPromocionPermanenteAplicador _aplicadorPuntos = new();



    public async Task<ResultadoAplicacionPromocionPermanente?> ProcesarAlFinalizarVentaAsync(

        Cliente cliente,

        decimal totalVenta,

        string codigoVenta)

    {

        var candidatas = await PromocionPermanenteEvaluador.EvaluarCandidatasAsync(

            _unitWork,

            cliente.Id,

            totalVenta,

            TipoRecompensaPromocion.PuntosExtra,

            persistirProgresoNCompras: true);



        var ganadora = PromocionPermanenteEvaluador.SeleccionarMejorPuntosExtra(candidatas);

        if (ganadora is null)

            return null;



        PromocionPermanenteEvaluador.ReiniciarContadorNCompras(ganadora);



        return await _aplicadorPuntos.AplicarAsync(cliente, ganadora.Promo, codigoVenta, _unitWork);

    }

}


