using HotChocolate.Authorization;
using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Entity;
using KafeYana.Domain.TiposDeDatos;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class UsuarioQuery
    {
        [Authorize]
        public async Task<DtoUsuarioDatos> Me([Service] IUsuarioRepositorio user, ClaimsPrincipal info)
        {

            var userId = info.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) throw new Exception("info no encontrado");

            var (Role, usuarioDb) = await user.Me(userId);

            if (usuarioDb == null || Role == string.Empty) throw new Exception("Usario no encontrado"); 

            var usaurio = new DtoUsuarioDatos
            {
                Nombre = usuarioDb.Nombre,
                Apellido = usuarioDb.Apellido,
                UserName = usuarioDb.UserName == null ? string.Empty : usuarioDb.UserName,
                Email = usuarioDb.Email == null ? string.Empty : usuarioDb.Email,
                Celular = usuarioDb.PhoneNumber == null ? string.Empty : usuarioDb.PhoneNumber,
                Estado = usuarioDb.LockoutEnabled,
                Rol = Role
            };

            return usaurio;
        }

        [Authorize(Roles = new[] { RolesKafe.Admin })]
        public async Task<IEnumerable<DtoUsuarioDatos>> Usuarios(
            [Service] UserManager<Usuario> _userManager)
        {
            var usuarios = await _userManager.Users.AsNoTracking().ToListAsync();

            var resultado = new List<DtoUsuarioDatos>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                resultado.Add(new DtoUsuarioDatos
                {
                    Nombre = $"{usuario.Nombre}",
                    Apellido = $"{usuario.Apellido}",
                    UserName = usuario.UserName == null ? string.Empty : usuario.UserName,
                    Email = usuario.Email!,
                    Celular = usuario.PhoneNumber == null ? string.Empty : usuario.PhoneNumber,
                    Rol = roles.FirstOrDefault() ?? "Sin rol",
                    Estado = usuario.LockoutEnd.HasValue && usuario.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return resultado;
        }
    }
}
