using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.Servicios.FacturacionImpresion;
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

            services.Configure<DatosEmpresaOptions>(
                configuration.GetSection(DatosEmpresaOptions.SeccionNombre));

            services.Configure<FacturaImpresoraOptions>(
                configuration.GetSection(FacturaImpresoraOptions.SeccionNombre));

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
            services.AddScoped<IRecepcionFacturaService, RecepcionFacturaService>();
            services.AddScoped<IAnulacionFacturaService, AnulacionFacturaService>();
            services.AddScoped<IReversionAnulacionFacturaService, ReversionAnulacionFacturaService>();
            services.AddScoped<IFacturaVentaSiatPreparer, FacturaVentaSiatPreparer>();
            services.AddScoped<IFacturaSiatEnvioService, FacturaSiatEnvioService>();
            services.AddScoped<IFacturaSiatAnulacionService, FacturaSiatAnulacionService>();
            services.AddScoped<IFacturaSiatReversionAnulacionService, FacturaSiatReversionAnulacionService>();
            services.AddScoped<IFacturaImpresionService, FacturaImpresionService>();
            services.AddSingleton<ICufGenerator, CufGenerator>();
            services.AddSingleton<IFacturaXmlGenerator, FacturaXmlGenerator>();

            return services;
        }
    }
}
