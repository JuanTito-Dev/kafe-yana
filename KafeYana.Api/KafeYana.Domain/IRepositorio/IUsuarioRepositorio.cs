using KafeYana.Core.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IUsuarioRepositorio
    {
        public Task<Usuario?> GetUsuario(string refreshtoken);
    }
}
