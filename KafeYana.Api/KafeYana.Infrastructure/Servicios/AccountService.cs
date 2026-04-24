using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.Exceptions.Usuarios;
using KafeYana.Application.IServicios;
using KafeYana.Core.Entities.Entity;
using KafeYana.Domain.Request;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Servicios
{
    public class AccountService : IAccountService
    {
        private readonly IAuthTokenProcesador _loggin;
        private readonly UserManager<Usuario> _usuarios;
        private readonly AppDbContext _db;

        public AccountService(IAuthTokenProcesador _loggin, UserManager<Usuario> _usuarios, AppDbContext _base)
        {
            this._usuarios = _usuarios;
            this._loggin = _loggin;
            _db = _base;
        }

        public async Task Register(RegisterRequest datos)
        {
            if (await _usuarios.FindByEmailAsync(datos.Email) != null)
            {
                throw new UsuarioExiste(datos.Email);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var user = Usuario.Crear(email: datos.Email, datos.Nombre, datos.Apellido, datos.NumeroPhone);

                var result = await _usuarios.CreateAsync(user, password: datos.Password);
                if (!result.Succeeded)
                {
                    throw new RegiterUsuarioFailException(result.Errors.Select(x => x.Description));
                }

                var rolResult = await _usuarios.AddToRoleAsync(user, RolesKafe.Cajero);
                if (!rolResult.Succeeded)
                {
                    throw new RegiterUsuarioFailException(rolResult.Errors.Select(x => x.Description));
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<DtoUsuarioAnswer> Login(LoginRequest datos)
        {
            var user = await _usuarios.FindByEmailAsync(datos.Email);

            if (user == null || !await _usuarios.CheckPasswordAsync(user, datos.Password))
            {
                throw new LoginFailException(datos.Email);
            }

            var role = await _usuarios.GetRolesAsync(user);

            var datosToken = new InfoUsuarioToken
            {
                Id = user.Id,
                Email = user.Email!,
                Nombre = user.Nombre!,
                Rol = role.FirstOrDefault() ?? string.Empty

            };

            var (jwt, expires) = _loggin.GetJwtToken(datosToken);

            var refresh = _loggin.RefreshToken();

            var refresExpires = DateTime.UtcNow.AddDays(7);

            var tokenInTheTable = new RefreshToken
            {
                Token = refresh,
                ExpiraEn = refresExpires,
                CreadoEn = DateTime.UtcNow,
                UserId = datosToken.Id,
                IsRevoked = false
            };

            await _db.RefreshTokens.AddAsync(tokenInTheTable);
            await _db.SaveChangesAsync();

            _loggin.WriteAunthHttpOnlyCookie("ACCESS_TOKEN", jwt, expires);
            _loggin.WriteAunthHttpOnlyCookie("REFRESH_TOKEN", refresh, refresExpires);

            var respuesta = new DtoUsuarioAnswer
            {
                Nombre = user.Nombre,
                Email = user.Email!,
                Rol = role.FirstOrDefault() ?? string.Empty
            };

            return respuesta;

        }

        public async Task<DtoUsuarioAnswer> RefreshTokenAsync(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new RefreshTokenExceptions("Token refresh no encontrado");
            }

            // 1. Buscar el token en BD
            var refreshToken = await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);

            // 2. Validar que existe, no está revocado y no expiró
            if (refreshToken == null ||
                refreshToken.IsRevoked ||
                refreshToken.ExpiraEn < DateTime.UtcNow)
            {
                throw new RefreshTokenExceptions("Refresh token inválido o expirado");
            }

            // 3. Revocar el token actual — no se reutiliza
            refreshToken.IsRevoked = true;

            // 4. Generar nuevos tokens
            var roles = await _usuarios.GetRolesAsync(refreshToken.User);

            var datosToken = new InfoUsuarioToken
            {
                Id = refreshToken.User.Id,
                Email = refreshToken.User.Email!,
                Nombre = refreshToken.User.Nombre!,
                Rol = roles.FirstOrDefault() ?? string.Empty
            };

            var (jwt, jwtExpires) = _loggin.GetJwtToken(datosToken);
            var nuevoRefresh = _loggin.RefreshToken();
            var refreshExpires = DateTime.UtcNow.AddDays(7);

            // 5. Guardar nuevo RefreshToken en BD
            refreshToken.IsRevoked = false;
            refreshToken.ExpiraEn = refreshExpires;
            refreshToken.CreadoEn = DateTime.UtcNow;
            refreshToken.Token = nuevoRefresh;
            await _db.SaveChangesAsync();

            // 6. Escribir nuevas cookies
            _loggin.WriteAunthHttpOnlyCookie("ACCESS_TOKEN", jwt, jwtExpires);
            _loggin.WriteAunthHttpOnlyCookie("REFRESH_TOKEN", nuevoRefresh, refreshExpires);

            var respuesta = new DtoUsuarioAnswer
            {
                Nombre = refreshToken.User.Nombre,
                Email = refreshToken.User.Email!,
                Rol = roles.FirstOrDefault() ?? string.Empty
            };

            return respuesta;
        }

        public async Task Logout(string? refreshToken)
        {
            // Borrar el refresh token de la BD si existe
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var token = await _db.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                if (token != null)
                {
                    _db.RefreshTokens.Remove(token);
                    await _db.SaveChangesAsync();
                }
            }

            // Borrar las cookies
            _loggin.DeleteAuthCookie("ACCESS_TOKEN");
            _loggin.DeleteAuthCookie("REFRESH_TOKEN");
        }
    }
}
