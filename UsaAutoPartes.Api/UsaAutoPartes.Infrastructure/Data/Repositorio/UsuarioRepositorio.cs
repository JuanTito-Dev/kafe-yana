using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly UserManager<Usuario> _Usuario;
        public UsuarioRepositorio(UserManager<Usuario> _Usuario)
        {
            this._Usuario = _Usuario;
        }
        public async Task<(Usuario? usuario, string? rol)> Me(string id)
        {
            var usuario = await _Usuario.FindByIdAsync(id);

            if(usuario is null) return (null, "");

            var rol = await _Usuario.GetRolesAsync(usuario);

            return (usuario, rol.FirstOrDefault());
        }
    }
}
