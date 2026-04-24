using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IOpcionRepositorio : IGenericRepositorio<Opcion>
    {
        Task<bool> Opciondeproducto(int Id, int Id_Opcion);
        Task<Opcion?> TraerOpcion(int? Id);
    }
}
