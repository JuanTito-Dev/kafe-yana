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

        public async Task<Usuario?> Me(string Id)
        {
            var usuario = await _Usuario.FindByIdAsync(Id);
            if (usuario is null) return null;

            return usuario;
        }
    }
}
