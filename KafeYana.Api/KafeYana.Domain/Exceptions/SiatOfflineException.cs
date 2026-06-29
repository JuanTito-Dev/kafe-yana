using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Exceptions
{
    /// <summary>
    /// Se lanza cuando el <c>ISiatConnectivityMonitor</c> tiene el circuito
    /// abierto (hay un evento significativo activo) y un consumidor intenta
    /// ejecutar una operación contra el SIN. Es la señal para que el flujo
    /// de venta derive a "modo contingencia" sin tocar la red.
    ///
    /// Transporta <see cref="CodigoSucursal"/> y <see cref="CodigoPuntoVenta"/>
    /// para que el consumidor (ej. <c>FacturaSiatEnvioService</c>) pueda
    /// resolver el <c>EventoSignificativoSiatId</c> activo vía
    /// <c>ISiatConnectivityMonitor.ObtenerEventoActivo(...)</c> sin tener que
    /// volver a inyectar el monitor en cada sitio de captura.
    ///
    /// NO es un error de negocio: representa una operación diferida. El cajero
    /// no debe ver mensaje de error; la venta se persiste con <c>TipoEmision=2</c>
    /// y queda pendiente de reenvío automático cuando el SIN vuelva.
    /// </summary>
    public class SiatOfflineException(
        string message,
        int codigoSucursal,
        int codigoPuntoVenta) : Exception(message)
    {
        public int CodigoSucursal { get; } = codigoSucursal;
        public int CodigoPuntoVenta { get; } = codigoPuntoVenta;
    }
}