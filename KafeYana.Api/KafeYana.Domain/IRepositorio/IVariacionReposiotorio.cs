using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IVariacionReposiotorio :  IGenericRepositorio<Variacion>
    {
        Task CrearOpcion(Opcion opcion);

        Task<Opcion?> TraerOpcion(int Id);

        Task DeleteOpcion(Opcion datos);

        Task<bool> ExisteOpcion(int Id);
    }
}
