using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IComboRepositorio : IGenericRepositorio<Producto>
    {
        Task<Producto?> GetCombo(int Id);

        IQueryable<Promocion> GetCombos();

        Task<Promocion?> TraerPromocion(int Id);
    }
}
