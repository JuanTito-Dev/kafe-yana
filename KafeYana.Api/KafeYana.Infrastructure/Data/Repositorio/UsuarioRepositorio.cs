using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Entity;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly AppDbContext _db;
        public UsuarioRepositorio(AppDbContext _db)
        {
            this._db = _db;
        }

        public Task<Usuario?> GetUsuario(string refreshtoken)
        {
            throw new NotImplementedException();
        }
    }
}
