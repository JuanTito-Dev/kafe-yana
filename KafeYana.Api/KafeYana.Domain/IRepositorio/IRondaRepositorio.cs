using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IRondaRepositorio : IGenericRepositorio<Ronda>
    {
        Task<int> Count(Expression<Func<Ronda, bool>> filtro);
    }
}
