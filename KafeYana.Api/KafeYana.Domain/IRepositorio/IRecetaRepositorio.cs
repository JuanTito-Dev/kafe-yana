using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IRecetaRepositorio : IGenericRepositorio<Receta>
    {
        Task<Receta?> GetReceta(int Id, bool includeProducto = false);

        IQueryable<Receta> GetRecetas();

        Task<Receta?> GetRectaByIdElaborado(int Id);
    }
}
