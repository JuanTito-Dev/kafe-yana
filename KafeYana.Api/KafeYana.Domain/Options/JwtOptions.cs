using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Options
{
    public class JwtOptions
    {
        public const string JwtOptionsKey = "JwtOptions";

        public string Secret { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public int Experice { get;set; }


    }
}
