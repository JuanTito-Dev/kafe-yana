using KafeYana.Application.Dtos.Autentication;
using KafeYana.Application.IServicios;
using KafeYana.Core.Entities.Entity;
using KafeYana.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace KafeYana.Infrastructure.Procesos
{
    public class AuthTokenProcesador : IAuthTokenProcesador
    {
        private readonly JwtOptions _jwt;
        private readonly IHttpContextAccessor _http;
        public AuthTokenProcesador(IOptions<JwtOptions> jwt, IHttpContextAccessor http)
        {
            _jwt = jwt.Value;
            _http = http;
        }

        public (string jwtToken, DateTime expiresAtUtc) GetJwtToken(InfoUsuarioToken datos)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));

            var credencials = new SigningCredentials(
                key,
                algorithm: SecurityAlgorithms.HmacSha512
                );

            var claims = new[]
            {
                new Claim(type: JwtRegisteredClaimNames.Sub, datos.Id.ToString()),
                new Claim(type: JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(type: JwtRegisteredClaimNames.Email, datos.Email!),
                new Claim(type: ClaimTypes.NameIdentifier, datos.Nombre),
                new Claim(type: ClaimTypes.Role, datos.Rol)
            };

            var expires = DateTime.UtcNow.AddMinutes(_jwt.Experice);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credencials);

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return (jwtToken, expires);
        }

        public string RefreshToken()
        {
            var numero = new byte[64];
            using var run = RandomNumberGenerator.Create();
            run.GetBytes(numero);
            return Convert.ToBase64String(numero);
        }

        public void WriteAunthHttpOnlyCookie(string NameToken, string Token, DateTime expiration)
        {
            var context = _http.HttpContext ?? throw new InvalidOperationException("No existe contexto HTTP");

            context.Response.Cookies.Append(
                NameToken,
                Token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = expiration,
                    IsEssential = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                }
                );
        }
    }
}
