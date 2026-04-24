using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.Dtos;
using UsaAutoPartes.Application.Dtos.Autentication;
using UsaAutoPartes.Application.Dtos.Authentication;
using UsaAutoPartes.Application.Exceptions.Autentication;
using UsaAutoPartes.Application.Exceptions.AuthenticationExceptions;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Application.IServicios;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Domain.Enum.CookieNames;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class AuthenticationRepositorio : IAuthenticationRepositorio
    {
        private readonly IAuthTokenProcessor _servicesToken;
        private readonly UserManager<Usuario> _usuarios;
        private readonly AppDbContext db;

        public AuthenticationRepositorio(IAuthTokenProcessor authToken, UserManager<Usuario> _usuarios, AppDbContext _db)
        {
            _servicesToken = authToken;
            this._usuarios = _usuarios;
            db = _db;
        }

        public async Task Register(RequestRegister datos)
        {
            var user = await _usuarios.FindByEmailAsync(datos.Email);

            if (user != null)
            {
                throw new UsuarioExisteException(datos.Email);
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var usuario = Usuario.Created(Email: datos.Email, datos.Nombre, datos.Apellido);

                var result = await _usuarios.CreateAsync(usuario, password: datos.Password);

                if (!result.Succeeded)
                {
                    throw new RegistroTransaccionFailException(result.Errors.Select(x => x.Description));
                }

                var rolresult = await _usuarios.AddToRoleAsync(usuario, UsuarioRoles.Admin.ToString());

                if (!rolresult.Succeeded)
                {
                    throw new RegistroTransaccionFailException(rolresult.Errors.Select(x => x.Description));
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;

            }
        }

        public async Task<DtoUsuarioDatos> Login(RequestLogin datos)
        {
            var user = await _usuarios.FindByEmailAsync(datos.Email);

            if (user == null || !await _usuarios.CheckPasswordAsync(user, password: datos.Password)) throw new LoginFailException(datos.Email);
            
            var Role = await _usuarios.GetRolesAsync(user);

            if (Role is null) throw new LoginFailException(datos.Email);

            var datosToke = new DtoUsuarioToken
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Email = user.Email!,
                Rol = Role.FirstOrDefault() ?? string.Empty,
            };

            var (jwt, expires) = _servicesToken.GenerateToken(datosToke);

            var eshToken = _servicesToken.GenerateRefresToken();

            var refreshExpires = DateTime.UtcNow.AddDays(7);

            var refresIntable = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = eshToken,
                UserId = user.Id,
                IsRevoked = false,
                CreadoEn = DateTime.UtcNow,
                ExpiraEn = refreshExpires
            };

            await db.RefreshTokens.AddAsync(refresIntable);

            await db.SaveChangesAsync();

            _servicesToken.WriteAuthCookie(CookieName: CookiesNames.access.ToString(), jwt, expires);
            _servicesToken.WriteAuthCookie(CookiesNames.accessreload.ToString(), eshToken, refreshExpires);

            return new DtoUsuarioDatos
            {
                Nombre = datosToke.Nombre,
                Correo = datosToke.Email,
                Rol = datosToke.Rol
            };
        }

        public async Task<DtoUsuarioDatos> RefreshTokenAsync(string? Token)
        {
            if (string.IsNullOrEmpty(Token)) throw new RefreshTokenFailException("Token no encontrado");

            var info = await db.RefreshTokens.Include(x => x.Usuario).FirstOrDefaultAsync(x => x.Token == Token);

            if (info == null || info.IsRevoked == true || info.ExpiraEn < DateTime.UtcNow)
            {
                throw new RefreshTokenFailException("Token no valido");
            }

            var datosToke = new DtoUsuarioToken
            {
                Id = info.Usuario.Id,
                Nombre = info.Usuario.Nombre,
                Email = info.Usuario.Email!,
                Rol = (await _usuarios.GetRolesAsync(info.Usuario)).FirstOrDefault() ?? string.Empty,
            };

            info.IsRevoked = true;

            var (jwt, expires) = _servicesToken.GenerateToken(datosToke);
            var eshToken = _servicesToken.GenerateRefresToken();
            var refreshExpires = DateTime.UtcNow.AddDays(7);

            info.Token = eshToken;
            info.ExpiraEn = refreshExpires;
            info.CreadoEn = DateTime.UtcNow;
            info.IsRevoked = false;
            
            await db.SaveChangesAsync();

            _servicesToken.WriteAuthCookie(CookieName: CookiesNames.access.ToString(), jwt, expires);
            _servicesToken.WriteAuthCookie(CookiesNames.accessreload.ToString(), eshToken, refreshExpires);

            return new DtoUsuarioDatos
            {
                Nombre = datosToke.Nombre,
                Correo = datosToke.Email,
                Rol = datosToke.Rol
            };
        }
    }
}
