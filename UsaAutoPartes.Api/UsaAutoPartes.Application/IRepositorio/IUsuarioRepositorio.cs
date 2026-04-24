using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IUsuarioRepositorio
    {
         Task<(Usuario? usuario, string? rol)> Me(string id);
    }
}
