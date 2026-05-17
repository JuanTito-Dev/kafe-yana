namespace KafeYana.Api.Hubs
{
    public interface IKafeYanaNotificador
    {
        Task NotificarMesaActualizada(MesaActualizadaPayload payload);
        Task NotificarNuevaRonda(NuevaRondaPayload payload);
        Task NotificarVentaProcesada(VentaPayload payload);
        Task NotificarPedidoParaLlevarActualizado(ParaLlevarPayload payload);
        Task NotificarStockActualizado(StockActualizadoPayload payload);
    }
}
