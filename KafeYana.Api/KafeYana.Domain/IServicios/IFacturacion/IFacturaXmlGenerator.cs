using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface IFacturaXmlGenerator
    {
        string Generar(Venta venta);
    }
}
