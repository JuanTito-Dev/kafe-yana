using Microsoft.AspNetCore.SignalR;

namespace KafeYana.Api.Hubs
{
    public class KafeYanaNotificador(IHubContext<KafeYanaHub> _hub) : IKafeYanaNotificador
    {
        public async Task NotificarMesaActualizada(MesaActualizadaPayload payload)
        {
            await _hub.Clients.Groups("salon", "caja")
                .SendAsync("MesaActualizada", payload);
        }

        public async Task NotificarNuevaRonda(NuevaRondaPayload payload)
        {
            await _hub.Clients.Group("salon").SendAsync("NuevaRonda", payload);
        }

        public async Task NotificarVentaProcesada(VentaPayload payload)
        {
            await _hub.Clients.Groups("salon", "caja")
                .SendAsync("VentaProcesada", payload);
        }

        public async Task NotificarPedidoParaLlevarActualizado(ParaLlevarPayload payload)
        {
            await _hub.Clients.Groups("salon", "caja")
                .SendAsync("PedidoParaLlevarActualizado", payload);
        }

        public async Task NotificarStockActualizado(StockActualizadoPayload payload)
        {
            await _hub.Clients.Group("salon").SendAsync("StockActualizado", payload);
        }
    }
}
