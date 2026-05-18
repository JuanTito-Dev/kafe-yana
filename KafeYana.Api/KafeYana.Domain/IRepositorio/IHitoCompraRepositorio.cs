using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IHitoCompraRepositorio : IGenericRepositorio<HitoCompra>
    {
        IQueryable<HitoCompra> GetHitos();
    }
}
