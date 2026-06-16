using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public static class FacturaSiatCobroPolicy
    {
        public static bool PermiteCompletarCobro(ResultadoEnvioFacturaSiatDto resultado)
        {
            if (EsValidadaPorSiat(resultado))
                return true;

            return EsPendientePorFalloComunicacion(resultado);
        }

        public static bool EsValidadaPorSiat(ResultadoEnvioFacturaSiatDto resultado) =>
            resultado.Enviado
            && resultado.Transaccion
            && resultado.EstadoSiat == FacturaEstado.Validada
            && resultado.CodigoEstado == (int)FacturaEstado.Validada;

        public static bool EsPendientePorFalloComunicacion(ResultadoEnvioFacturaSiatDto resultado) =>
            !resultado.Enviado
            && resultado.EstadoSiat == FacturaEstado.Pendiente;

        public static string MensajeRechazoCobro(ResultadoEnvioFacturaSiatDto resultado)
        {
            if (!string.IsNullOrWhiteSpace(resultado.ErrorMensaje))
                return $"El cobro no se completó porque el SIAT rechazó la factura: {resultado.ErrorMensaje}";

            if (!string.IsNullOrWhiteSpace(resultado.CodigoDescripcion))
                return $"El cobro no se completó porque el SIAT rechazó la factura: {resultado.CodigoDescripcion}";

            return "El cobro no se completó porque el SIAT rechazó la factura.";
        }
    }
}
