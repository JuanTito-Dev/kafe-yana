using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Dtos.VentaDtos;



namespace KafeYana.Api.Helpers

{

    internal static class VentaRespuestaHelper

    {

        public static object ConstruirRespuestaCobro(
            ResultadoProcesarVenta resultado,
            ResultadoEnvioFacturaSiatDto? envioSiat = null,
            string mensajeBase = "Venta procesada correctamente")

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

                CodigoVenta = resultado.Venta.Cuf,

                VentaId = resultado.Venta.Id,

                NumeroFactura = resultado.Venta.NumeroFactura,
                Facturado = resultado.Venta.Facturado,

                EstadoSiat = envioSiat?.EstadoSiat ?? resultado.Venta.EstadoSiat,

                CodigoRecepcion = envioSiat?.CodigoRecepcion ?? resultado.Venta.CodigoRecepcion,

                SiatAceptada = envioSiat?.Transaccion
                    ?? resultado.Venta.EstadoSiat == KafeYana.Domain.TiposDeDatos.FacturaEstado.Validada,

                ErrorSiat = envioSiat?.ErrorMensaje ?? resultado.Venta.ErrorMensaje,

                CodigoHash = resultado.Venta.CodigoHash,

                Siat = envioSiat is null ? null : new

                {

                    envioSiat.Enviado,

                    envioSiat.Transaccion,

                    envioSiat.EstadoSiat,

                    envioSiat.CodigoEstado,

                    envioSiat.CodigoRecepcion,

                    envioSiat.CodigoDescripcion,

                    envioSiat.ErrorMensaje,

                    envioSiat.CodigosRespuesta

                },

                XmlGenerado = !string.IsNullOrWhiteSpace(resultado.Venta.XmlBase64),

                SubtotalPedido = resultado.Venta.MontoTotal + (resultado.Venta.DescuentoAdicional ?? 0m),

                TotalCobrado = resultado.Venta.MontoTotal,

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


