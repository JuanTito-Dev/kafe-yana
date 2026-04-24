using HotChocolate.Authorization;
using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Entity;
using Mapster;
using Microsoft.AspNetCore.Identity;
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

            var usuarioDb = await user.Me(userId);

            if (usuarioDb == null) throw new Exception("Usario no encontrado");

            var usaurio = new DtoUsuarioDatos
            {
                Nombre = usuarioDb.Nombre,
                Apellido = usuarioDb.Apellido,
                UserName = usuarioDb.UserName == null ? string.Empty : usuarioDb.UserName,
                Email = usuarioDb.Email == null ? string.Empty : usuarioDb.Email,
                Celular = usuarioDb.PhoneNumber == null ? string.Empty : usuarioDb.PhoneNumber,
                Estado = usuarioDb.LockoutEnabled

            };

            return usaurio;
        }
    }
}
