using KafeYana.Application.IServicios;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Application.IRepositorio;
using KafeYana.Infrastructure.BackgroundServices;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Options;
using KafeYana.Infrastructure.Servicios;
using KafeYana.Infrastructure.Servicios.Facturacion;
using KafeYana.Infrastructure.Servicios.FacturacionImpresion;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using KafeYana.Infrastructure.Data.Repositorio;
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

            // Detector automático de caída del SIAT (módulo de contingencia).
            services.Configure<DetectorOptions>(
                configuration.GetSection(DetectorOptions.SeccionNombre));

            // Probe periódico que rompe el chicken-and-egg del monitor: si
            // hay contingencias Activas y nadie factura/sincroniza, este
            // BackgroundService detecta recuperación de SIAT y dispara
            // ReportarExitoAsync → Pieza 3b → cierre de contingencia.
            services.Configure<SiatConnectivityProbeOptions>(
                configuration.GetSection(SiatConnectivityProbeOptions.SeccionNombre));

            // Impresoras térmicas: la sección unificada `Impresoras` cubre
            // comandas/cuentas/recibos Y la factura fiscal (selección por
            // destino enviada desde el frontend).
            services.Configure<ImpresoraOptions>(
                configuration.GetSection(ImpresoraOptions.Key));

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
            services.AddScoped<IFechaHoraSiatService, FechaHoraSiatService>();
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

            // Nota de Crédito/Débito (SIAT — sector 24, tipoFactura 3)
            services.AddScoped<INotaAjusteXmlGenerator, NotaAjusteXmlGenerator>();
            services.AddScoped<IRecepcionNotaAjusteService, RecepcionNotaAjusteService>();
            services.AddScoped<INotaAjusteSiatPreparer, NotaAjusteSiatPreparer>();
            services.AddScoped<INotaAjusteSiatEnvioService, NotaAjusteSiatEnvioService>();
            services.AddScoped<INotaAjusteAnulacionService, NotaAjusteAnulacionService>();
            services.AddScoped<INotaAjusteReversionAnulacionService, NotaAjusteReversionAnulacionService>();
            services.AddScoped<INotaAjusteSiatAnulacionService, NotaAjusteSiatAnulacionService>();
            services.AddScoped<INotaAjusteSiatReversionAnulacionService, NotaAjusteSiatReversionAnulacionService>();

            // Sincronización de catálogos del SIAT
            // (Scoped porque depende de ICuisService que también es Scoped)
            services.AddScoped<SincronizadorCatActividades>();
            services.AddScoped<SincronizadorCatDocumentoSector>();
            services.AddScoped<SincronizadorCatMotivoAnulacion>();
            services.AddScoped<SincronizadorCatActividadDocumentoSector>();
            services.AddScoped<SincronizadorCatLeyenda>();
            services.AddScoped<SincronizadorCodigosSiat>();
            services.AddScoped<SincronizadorCatEventoSignificativo>();
            services.AddScoped<SincronizadorCatPaisOrigen>();
            services.AddScoped<SincronizadorCatTipoDocumentoIdentidad>();
            services.AddScoped<SincronizadorCatTipoEmision>();
            services.AddScoped<SincronizadorCatMetodosPago>();
            services.AddScoped<SincronizadorCatUnidadMedida>();

            // Resolver compartido del CAEB vigente. Usado por VentaServices y por
            // los preparers SIAT (FacturaVentaSiatPreparer / NotaAjusteSiatPreparer)
            // para validar la matriz Actividad↔DocumentoSector.
            services.AddScoped<ICatActividadResolver, CatActividadResolver>();

            // Resolver de la leyenda obligatoria filtrada por el CAEB del operador.
            // Reemplaza al antiguo LeyendaSiatService hardcodeado.
            services.AddScoped<ICatLeyendaResolver, CatLeyendaResolver>();

            // Flujo de contingencia SIAT (registroEventoSignificativo + estado).
            // Ver [[kafeyana-contingencia-siat]].
            services.AddScoped<IEventoSignificativoSiatRepositorio, EventoSignificativoSiatRepositorio>();
            services.AddScoped<IEventoSignificativoSiatService, EventoSignificativoSiatService>();

            // Monitor singleton de conectividad SIAT + servicios de contingencia.
            services.AddSingleton<ISiatConnectivityMonitor, SiatConnectivityMonitor>();

            // Logger singleton a archivo rotativo por día para diagnóstico del
            // flujo de contingencia. Ver [[kafeyana-contingencia-siat]]. Lifetime
            // Singleton para mantener el ciclo de vida del StreamWriter y los
            // locks por archivo en memoria.
            services.AddSingleton<IContingenciaDebugLogService, ContingenciaDebugLogService>();

            // Reenvío de facturas emitidas en contingencia (cuando el SIAT vuelve).
            services.AddScoped<ReenvioFacturasContingenciaService>();

            // Bootstrap que hidrata el monitor al boot y reanuda ventas pendientes
            // de contingencias que ya estaban activas en BD.
            services.AddHostedService<ContingencyBootstrapHostedService>();

            // BackgroundService que consume los eventos de recuperación y procesa
            // el reenvío secuencial de facturas pendientes (un evento a la vez para
            // no saturar el SIAT).
            services.AddHostedService<ContingencyResendHostedService>();

            services.AddHostedService<SincronizacionCatActividadesHostedService>();
            services.AddHostedService<SincronizacionMotivoAnulacionHostedService>();
            services.AddHostedService<SincronizacionLeyendaHostedService>();
            services.AddHostedService<SincronizacionCodigosSiatHostedService>();
            services.AddHostedService<SincronizacionEventoSignificativoHostedService>();
            services.AddHostedService<SincronizacionPaisOrigenHostedService>();
            services.AddHostedService<SincronizacionTipoDocumentoIdentidadHostedService>();
            services.AddHostedService<SincronizacionTipoEmisionHostedService>();
            services.AddHostedService<SincronizacionMetodosPagoHostedService>();
            services.AddHostedService<SincronizacionUnidadMedidaHostedService>();

            // Probe periódico de reachability de SIAT. Rompe el ciclo de
            // "contingencia Activa → cortocircuito → nadie detecta recuperación".
            services.AddHostedService<SiatConnectivityProbeService>();

            return services;
        }
    }
}
