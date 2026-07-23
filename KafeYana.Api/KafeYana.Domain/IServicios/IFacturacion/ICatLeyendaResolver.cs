using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Resuelve las leyendas obligatorias del SIAT filtradas por la actividad
    /// económica (CAEB) del operador.
    ///
    /// Las leyendas viven en <c>CatLeyendas</c>, sincronizadas por
    /// <c>SincronizadorCatLeyenda</c> (filtradas por la CAEB principal al
    /// persistir). Este resolver las lee y devuelve UNA al azar para incluir
    /// en el campo <c>&lt;leyenda&gt;</c> del XML de factura / nota de ajuste.
    ///
    /// Lanza <see cref="KafeYana.Application.Exceptions.VentaException"/> si la
    /// tabla no tiene leyendas para el CAEB vigente — el operador debe ejecutar
    /// <c>POST /api/catalogos/sincronizar-leyendas</c> antes de facturar.
    /// Cumple el contrato de fail-closed de [[kafeyana-vservices-throw-on-missing-config]].
    /// </summary>
    public interface ICatLeyendaResolver
    {
        /// <summary>
        /// Devuelve una descripción de leyenda al azar, elegida entre las que
        /// aplican al <paramref name="codigoActividad"/> indicado.
        /// </summary>
        Task<string> ObtenerAleatoriaAsync(string codigoActividad, CancellationToken ct = default);
    }
}
