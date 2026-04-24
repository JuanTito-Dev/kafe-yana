using KafeYana.Application.Dtos.Autentication;
using KafeYana.Domain.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios
{
    public interface IAccountService
    {
        Task Register(RegisterRequest datos);

        Task<DtoUsuarioAnswer> Login(LoginRequest datos);

        Task<DtoUsuarioAnswer> RefreshTokenAsync(string? token);

        Task Logout(string? refreshToken);
    }
}
