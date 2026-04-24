using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IInsumoRepositorio : IGenericRepositorio<Insumo>
    {
        IQueryable<Insumo> GetInsumos();
    }
}
