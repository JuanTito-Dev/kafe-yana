using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Genera el XML raíz &lt;notaFiscalComputarizadaCreditoDebito&gt; con cabecera + detalles.
    /// Espejo de IFacturaXmlGenerator para facturas del sector 1.
    /// </summary>
    public interface INotaAjusteXmlGenerator
    {
        string Generar(NotaAjuste nota);
    }
}
