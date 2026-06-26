using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios
{
    /// <summary>
    /// Resuelve el código CAEB (actividad económica) vigente que debe usarse
    /// en los documentos SIAT (factura / nota de ajuste).
    ///
    /// Cadena de selección:
    ///   1) Fila de <c>CatActividades</c> con <c>TipoActividad == "P"</c>
    ///      (Principal marcada por el SIN).
    ///   2) Fila cuyo <c>CodigoCaeb</c> coincida con
    ///      <c>DatosEmpresaOptions.CodigoActividad</c> (appsettings.json).
    ///   3) Primera fila de la tabla, con warning (caso degradado).
    ///
    /// Lanza <see cref="KafeYana.Application.Exceptions.CatalogoNoSincronizadoException"/>
    /// si la tabla está completamente vacía, preservando el contrato previo
    /// que tenía <c>VentaServices.ResolverActividadEconomica()</c>.
    /// </summary>
    public interface ICatActividadResolver
    {
        /// <summary>
        /// Devuelve el CAEB vigente para emitir documentos SIAT.
        /// </summary>
        Task<string> ResolverCaebVigenteAsync(CancellationToken ct = default);
    }
}