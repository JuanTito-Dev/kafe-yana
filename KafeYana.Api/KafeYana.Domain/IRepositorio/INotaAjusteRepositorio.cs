using System.Linq;
using System.Threading.Tasks;
using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface INotaAjusteRepositorio : IGenericRepositorio<NotaAjuste>
    {
        IQueryable<NotaAjuste> NotaAjusteQuery();

        /// <summary>Correlativo SIAT: MAX(NumeroNotaCreditoDebito) + 1 entre todas las notas.</summary>
        Task<long> SiguienteNumeroNotaCreditoDebitoAsync();

        Task<NotaAjuste?> TraerNotaAjusteConDetallesAsync(int id);

        Task<IReadOnlyList<NotaAjuste>> ListarPorVentaAsync(int ventaId);
    }
}
