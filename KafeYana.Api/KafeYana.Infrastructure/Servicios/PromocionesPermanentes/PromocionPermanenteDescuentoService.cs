using KafeYana.Application.Dtos.VentaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.PromocionesPermanentes;

public sealed class PromocionPermanenteDescuentoService(IUnitWork _unitWork) : IPromocionPermanenteDescuentoService
{
    public async Task<DtoDescuentosPedidoRespuesta> ObtenerDescuentosPedidoAsync(int idPedido, int idCliente)
    {
        var pedido = await _unitWork.Pedidos.FindByIdAsync(idPedido);
        if (pedido is null)
            throw new InventarioException("Pedido no encontrado");

        await ValidarClientePedidoAsync(pedido, idCliente);

        var subtotal = pedido.Total;
        var candidatas = await PromocionPermanenteEvaluador.EvaluarCandidatasAsync(
            _unitWork,
            idCliente,
            subtotal,
            TipoRecompensaPromocion.Descuento,
            persistirProgresoNCompras: false);

        var descuentos = candidatas
            .Select(c => MapDescuento(c.Promo, subtotal))
            .OrderByDescending(d => d.MontoDescuento)
            .ThenByDescending(d => d.PorcentajeDescuento)
            .ToList();

        return new DtoDescuentosPedidoRespuesta
        {
            Id_Pedido              = idPedido,
            Id_Cliente             = idCliente,
            SubtotalPedido         = subtotal,
            HayDescuentoDisponible = descuentos.Count > 0,
            DescuentosDisponibles  = descuentos,
            DescuentoRecomendado   = descuentos.FirstOrDefault()
        };
    }

    public async Task<ResultadoAplicacionDescuentoPromocion?> AplicarDescuentoAsync(
        Cliente cliente,
        decimal subtotalPedido,
        string codigoVenta)
    {
        var candidatas = await PromocionPermanenteEvaluador.EvaluarCandidatasAsync(
            _unitWork,
            cliente.Id,
            subtotalPedido,
            TipoRecompensaPromocion.Descuento,
            persistirProgresoNCompras: true);

        var mejor = PromocionPermanenteEvaluador.SeleccionarMejorDescuento(candidatas, subtotalPedido);
        if (mejor is null)
            return null;

        var (candidata, montoDescuento) = mejor.Value;
        var promo = candidata.Promo;

        PromocionPermanenteEvaluador.ReiniciarContadorNCompras(candidata);

        await _unitWork.historialPromocionPermanentes.Crear(new HistorialPromocionPermanente
        {
            Id_Cliente             = cliente.Id,
            Id_PromocionPermanente = promo.Id,
            CodigoVenta            = codigoVenta,
            TipoRecompensa         = promo.TipoRecompensa,
            ValorRecompensa        = promo.ValorRecompensa,
            TipoCondicion          = promo.TipoCondicion,
            ValorCondicion         = promo.ValorCondicion,
            Fecha                  = DateTime.UtcNow
        });

        var totalConDescuento = subtotalPedido - montoDescuento;

        return new ResultadoAplicacionDescuentoPromocion
        {
            IdPromocion         = promo.Id,
            NombrePromocion     = promo.Nombre,
            PorcentajeDescuento = promo.ValorRecompensa,
            MontoDescuento      = montoDescuento,
            SubtotalPedido      = subtotalPedido,
            TotalConDescuento   = totalConDescuento,
            Mensaje             =
                $"Se aplicó un descuento del {promo.ValorRecompensa}% ({montoDescuento:F2}) por la promoción \"{promo.Nombre}\"."
        };
    }

    private static DtoDescuentoDisponible MapDescuento(PromocionPermanente promo, decimal subtotal)
    {
        var monto = PromocionPermanenteEvaluador.CalcularMontoDescuento(subtotal, promo.ValorRecompensa);
        return new DtoDescuentoDisponible
        {
            IdPromocion         = promo.Id,
            Nombre              = promo.Nombre,
            TipoCondicion       = promo.TipoCondicion,
            ValorCondicion      = promo.ValorCondicion,
            PorcentajeDescuento = promo.ValorRecompensa,
            MontoDescuento      = monto,
            TotalConDescuento   = subtotal - monto
        };
    }

    private async Task ValidarClientePedidoAsync(Pedido pedido, int idCliente)
    {
        if (pedido.Id_Cliente is not null && pedido.Id_Cliente != idCliente)
            throw new InventarioException("El cliente no corresponde al pedido");

        if (await _unitWork.clientes.GetCliente(idCliente) is null)
            throw new InventarioException("Cliente no encontrado");
    }
}
