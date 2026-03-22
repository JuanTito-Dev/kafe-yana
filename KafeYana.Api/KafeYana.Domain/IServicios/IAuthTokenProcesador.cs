using KafeYana.Application.Dtos.Autentication;
using KafeYana.Core.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios
{
    public interface IAuthTokenProcesador
    {
        public (string jwtToken, DateTime expiresAtUtc) GetJwtToken(InfoUsuarioToken datos);

        public string RefreshToken();

        public void WriteAunthHttpOnlyCookie(string NameToken, string Token, DateTime expiration);
    }
}
