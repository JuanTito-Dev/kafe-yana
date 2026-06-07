using KafeYana.Domain.Entities.Facturacion;

namespace KafeYana.Application.IRepositorio
{
    public interface ICodigoSiatRepositorio : IGenericRepositorio<CodigoSiat>
    {
        IQueryable<CodigoSiat> Buscar(
            string? codigoProducto = null,
            string? codigoActividad = null,
            string? descripcionProducto = null,
            string? descripcionActividad = null,
            string? termino = null);
    }
}
