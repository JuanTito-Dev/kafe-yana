using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Core.Entities.Entity
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public required string Token { get; set; } = string.Empty;

        public required DateTime ExpiraEn { get; set; }

        public required DateTime CreadoEn { get; set; }

        public bool IsRevoked { get; set; }

        public required Guid UserId { get; set; }

        public Usuario User { get; set; } = null!;
    }
}


