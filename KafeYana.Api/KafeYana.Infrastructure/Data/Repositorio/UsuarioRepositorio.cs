using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Entity;
using Microsoft.AspNetCore.Identity;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly UserManager<Usuario> _Usuario;
        public UsuarioRepositorio(UserManager<Usuario> _Usuario)
        {
            this._Usuario = _Usuario;
        }

        public async Task<(string role ,Usuario? usuario)> Me(string Id)
        {
            var usuario = await _Usuario.FindByIdAsync(Id);

            if (usuario == null) return (string.Empty, null);

            var Role = await _Usuario.GetRolesAsync(usuario);

            return (Role.FirstOrDefault() ?? string.Empty, usuario);
        }
    }
}
