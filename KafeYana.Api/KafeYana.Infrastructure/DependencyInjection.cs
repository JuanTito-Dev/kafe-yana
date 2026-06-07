using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Configuración del SIAT
            services.Configure<SiatOptions>(
                configuration.GetSection(SiatOptions.SeccionNombre));

            // 2. HttpClient con la URL base y timeout configurados
            services.AddHttpClient<SiatHttpClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<SiatOptions>>().Value;
                client.BaseAddress = new Uri(opts.UrlBase);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSegundos);
            });

            // Servicios de facturación — Scoped (usan EF Core / BD por request)
            services.AddScoped<ICuisService, CuisService>();
            services.AddScoped<ICufdService, CufdService>();
            services.AddScoped<IVerificaNitService, VerificaNitService>();

            return services;
        }
    }
}
