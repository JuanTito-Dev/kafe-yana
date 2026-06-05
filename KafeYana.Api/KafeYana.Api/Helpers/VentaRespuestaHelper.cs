using KafeYana.Application.Dtos.VentaDtos;



namespace KafeYana.Api.Helpers

{

    internal static class VentaRespuestaHelper

    {

        public static object ConstruirRespuestaCobro(ResultadoProcesarVenta resultado, string mensajeBase = "Venta procesada correctamente")

        {

            var promoPuntos = resultado.PromocionPermanente;

            var descuento   = resultado.DescuentoPromocion;



            var mensaje = mensajeBase;

            if (descuento is not null)

                mensaje = $"{mensaje} {descuento.Mensaje}";

            if (promoPuntos is not null)

                mensaje = $"{mensaje} {promoPuntos.Mensaje}";



            return new

            {

                message = mensaje.Trim(),

                PuntosPorVenta = resultado.PuntosPorVenta,

                PuntosPromocionPermanente = promoPuntos?.PuntosExtra ?? 0,

                PromocionPermanente = promoPuntos is null

                    ? null

                    : new

                    {

                        promoPuntos.NombrePromocion,

                        promoPuntos.PuntosExtra,

                        promoPuntos.Mensaje

                    },

                AplicoDescuento = descuento is not null,

                MontoDescuento = descuento?.MontoDescuento ?? 0m,

                PorcentajeDescuento = descuento?.PorcentajeDescuento,

                CodigoVenta = resultado.Venta.Codigo,

                SubtotalPedido = resultado.Venta.Subtotal,

                TotalCobrado = resultado.Venta.Total,

                PromocionDescuento = descuento is null

                    ? null

                    : new

                    {

                        descuento.IdPromocion,

                        descuento.NombrePromocion,

                        descuento.PorcentajeDescuento,

                        descuento.MontoDescuento,

                        descuento.TotalConDescuento,

                        descuento.Mensaje

                    }

            };

        }

    }

}


