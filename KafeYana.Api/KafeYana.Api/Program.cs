using KafeYana.Api.DataLoaders;
using KafeYana.Api.Hubs;
using QuestPDF.Infrastructure;
using KafeYana.Api.Filters;
using KafeYana.Api.GraphQLMap;
using KafeYana.Api.GraphQLMap.Filtering;
using KafeYana.Api.GraphQLMap.Types;
using KafeYana.Api.Middlewares;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Core.Entities.Entity;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Data.Repositorio;
using KafeYana.Infrastructure.Options;
using KafeYana.Infrastructure.Procesos;
using KafeYana.Infrastructure.Servicios;
using KafeYana.Infrastructure.Servicios.PromocionesPermanentes;
using KafeYana.Infrastructure.Servicios.PromocionesTemporada;
using KafeYana.Infrastructure.Servicios.HitosCompra;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using KafeYana.Infrastructure;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Query = KafeYana.Api.GraphQLMap.Query;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

//Servicios

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.JwtOptionsKey));

//Conexion Base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DataBase:Conexion"])
);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DataBase:Conexion"]),
    ServiceLifetime.Scoped);

//Configuracion Identity

builder.Services.AddDataProtection();

builder.Services.AddIdentityCore<Usuario>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 5;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = false;
}).AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

//Configuracion jwt refreshtoken

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opy =>
{
    var jwtop = builder.Configuration.GetSection(JwtOptions.JwtOptionsKey)
    .Get<JwtOptions>() ?? throw new ArgumentException(nameof(JwtOptions));

    opy.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtop.Issuer,
        ValidAudience = jwtop.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtop.Secret))
    };


    opy.Events = new JwtBearerEvents
    {
        OnMessageReceived = Context =>
        {
            if (!Context.Request.Path.StartsWithSegments("/api/Aunth/RefreshToken"))
            {
                Context.Token = Context.Request.Cookies["ACCESS_TOKEN"];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSignalR();

builder.Services.AddMapster();

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<ExceptionGlobal>();

var origin = builder.Configuration.GetSection("Cors:Origins")
    .Get<string[]>() ?? [];

builder.Services.AddCors( x =>
{
    x.AddPolicy("CorsPoliticy", polity =>
    {
        polity
            .WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddGraphQLServer()
    .RegisterDbContextFactory<AppDbContext>()
    .AddQueryType<Query>(d => d.Name("Query"))
    .AddTypeExtension<AjustesQuery>()
    .AddTypeExtension<CategoriaQuery>()
    .AddTypeExtension<ClienteQuery>()
    .AddTypeExtension<ComboQuery>()
    .AddTypeExtension<CompradoQuery>()
    .AddTypeExtension<ElaboradoQuery>()
    .AddTypeExtension<InsumoQuery>()
    .AddTypeExtension<RecetaQuery>()
    .AddTypeExtension<UsuarioQuery>()
    .AddTypeExtension<MesaQuery>()
    .AddTypeExtension<VentaQuery>()
    .AddTypeExtension<ProductoMovimientosQuery>()
    .AddTypeExtension<InsumoMovimientosQuery>()
    .AddTypeExtension<ParaLlevarQuery>()
    .AddTypeExtension<CajaQuery>()
    .AddTypeExtension<CajaHistorialQuery>()
    .AddTypeExtension<OrdenCompraQuery>()
    .AddTypeExtension<HistorialPuntosQuery>()
    .AddTypeExtension<ProductoCanjeableQuery>()
    .AddTypeExtension<PromocionPermanenteQuery>()
    .AddTypeExtension<PromocionTemporadaQuery>()
    .AddTypeExtension<HitoCompraQuery>()
    .AddTypeExtension<HistorialReferidoQuery>()
    .AddTypeExtension<CodigoSiatQuery>()
    .AddType<AjusteType>()
    .AddType<CategoriaType>()
    .AddType<ClienteType>()
    .AddType<ComboType>()
    .AddType<CompradoType>()
    .AddType<DetalleType>()
    .AddType<ElaboradoType>()
    .AddType<InsumoType>()
    .AddType<OpcionType>()
    .AddType<ProductoType>()
    .AddType<PromocionDetalleType>()
    .AddType<RecetaType>()
    .AddType<VariacionType>()
    .AddType<MesaType>()
    .AddType<PedidoType>()
    .AddType<RondaType>()
    .AddType<DetalleRondaType>()
    .AddType<DetalleRondaComboItemType>()
    .AddType<VentaType>()
    .AddType<DetallePagoType>()
    .AddType<NotaAjusteType>()
    .AddType<NotaAjusteDetalleType>()
    .AddType<CategoriaForProductosType>()
    .AddType<ProductoMovientosType>()
    .AddType<InsumoMovimientoType>()
    .AddType<ParaLlevarType>()
    .AddType<CajaType>()
    .AddType<CajaMovimientoType>()
    .AddType<CajaHistorialType>()
    .AddType<CajaHistorialMovimientoType>()
    .AddType<Detalle_Ronda_OpcionType>()
    .ModifyRequestOptions(opt => 
    { 
        opt.IncludeExceptionDetails = true;

    })
    .ModifyCostOptions(o => o.EnforceCostLimits = false)
    .AddFiltering<CaseInsensitiveFilteringConvention>()
    .AddProjections()
    .AddSorting()
    .AddAuthorization()
    .AddTypeExtension<ProveedorQuery>()
    .AddTypeExtension<ReporteCajaQuery>()
    .AddTypeExtension<ReporteProductoQuery>()
    .AddType<ProveedorType>()
    .AddType<HistorialPuntosType>()
    .AddType<ProductoCanjeableType>()
    .AddType<PromocionPermanenteType>()
    .AddType<PromocionTemporadaType>()
    .AddType<PromocionTemporadaProductoCanjeableType>()
    .AddType<HitoCompraType>()
    .AddType<HistorialReferidoType>()
    .AddType<CodigoSiatType>()
    .AddDataLoader<PromocionCantidadProducibleDataLoader>()
    .AddDataLoader<RecetaCantidadProducibleDataLoader>();

//Servicios
builder.Services.Configure<ImpresoraOptions>(builder.Configuration.GetSection(ImpresoraOptions.Key));
builder.Services.AddScoped<IImpresoraService, ImpresoraService>();

builder.Services.Configure<CloudflareR2Options>(builder.Configuration.GetSection(CloudflareR2Options.Key));
builder.Services.AddSingleton<IR2StorageService, R2StorageService>();
builder.Services.AddScoped<IProductoImagenService, ProductoImagenService>();
builder.Services.AddScoped<IQrImagenService, QrImagenService>();

builder.Services.AddScoped<IAuthTokenProcesador, AuthTokenProcesador>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IVentaServices, VentaServices>();
builder.Services.AddScoped<ICobroPedidoService, CobroPedidoService>();
builder.Services.AddScoped<ISubVentaService, SubVentaService>();
builder.Services.AddScoped<IPromocionPermanenteVentaService, PromocionPermanenteVentaService>();
builder.Services.AddScoped<IPromocionPermanenteDescuentoService, PromocionPermanenteDescuentoService>();
builder.Services.AddScoped<IPromocionPermanenteProductoGratisService, PromocionPermanenteProductoGratisService>();
builder.Services.AddScoped<IPromocionTemporadaReclamoService, PromocionTemporadaReclamoService>();
builder.Services.AddScoped<IHitoCompraReclamoService, HitoCompraReclamoService>();
builder.Services.AddScoped<IInventarioCanjeService, InventarioCanjeService>();
builder.Services.AddScoped<ICanjeProductoService, CanjeProductoService>();
builder.Services.AddScoped(typeof(IGenericRepositorio<>), typeof(GenericRepositorio<>));
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IElaboradoRepositorio, ElaboradoRepositorio>();
builder.Services.AddScoped<IComboRepositorio, ComboRepositorio>();
builder.Services.AddScoped<IInsumoRepositorio, InsumoRepositorio>();
builder.Services.AddScoped<IRecetaRepositorio, RecetaRepositorio>();
builder.Services.AddScoped<IVariacionReposiotorio, VariacionRepositorio>();
builder.Services.AddScoped<IClienteRespositorio, ClienteRepositorio>();
builder.Services.AddScoped<ICatPaisOrigenRepositorio, CatPaisOrigenRepositorio>();
builder.Services.AddScoped<IAjusteStockRepositorio, AjusteStockRepositorio>();
builder.Services.AddScoped<IMesaRepositorio, MesaRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IRondaRepositorio, RondaRepositorio>();
builder.Services.AddScoped<ISubVentaRepositorio, SubVentaRepositorio>();
builder.Services.AddScoped<IVentaRepositorio, VentaRepositorio>();
builder.Services.AddScoped<INotaAjusteRepositorio, NotaAjusteRepositorio>();
builder.Services.AddScoped<IOpcionRepositorio, OpcionRepositorio>();
builder.Services.AddScoped<IUnitWork, UnitWork>();
builder.Services.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
builder.Services.AddScoped<IDetalle_RondaRepositorio, Detalle_RondaRepositorio>();
builder.Services.AddScoped<IPedidoInventarioCompromisoRepositorio, PedidoInventarioCompromisoRepositorio>();
builder.Services.AddScoped<IInventarioPedidoCompromisoService, InventarioPedidoCompromisoService>();
builder.Services.AddScoped<Detalle_RondaService>();
builder.Services.AddScoped<IRondaPedidoService, RondaPedidoService>();
builder.Services.AddScoped<ReporteInventarioService>();
builder.Services.AddScoped<ReporteCajaService>();
builder.Services.AddScoped<ReporteProductosService>();
builder.Services.AddScoped<IProductoMovimientoRepositorio, ProductoMovimientoRepositorio>();
builder.Services.AddScoped<IInsumoMovimientoRepositorio, InsumoMovimientoRepositorio>();
builder.Services.AddScoped<IParaLlevarRepositorio, ParaLlevarRepositorio>();
builder.Services.AddScoped<ICajaRepositorio, CajaRepositorio>();
builder.Services.AddScoped<ICajaMovimientoRepositorio, CajaMovimientoRepositorio>();
builder.Services.AddScoped<ICajaHistorialRepositorio, CajaHistorialRepositorio>();
builder.Services.AddScoped<ICajaHistorialMovimientoRepositorio, CajaHistorialMovimientoRepositorio>();
builder.Services.AddScoped<IOrdenCompraRepositorio, OrdenCompraRepositorio>();
builder.Services.AddScoped<IProductoCanjeableRepositorio, ProductoCanjeableRepositorio>();
builder.Services.AddScoped<IPromocionPermanenteRepositorio, PromocionPermanenteRepositorio>();
builder.Services.AddScoped<IPromocionPermanenteProgresoRepositorio, PromocionPermanenteProgresoRepositorio>();
builder.Services.AddScoped<IHistorialPromocionPermanenteRepositorio, HistorialPromocionPermanenteRepositorio>();
builder.Services.AddScoped<IPromocionTemporadaRepositorio, PromocionTemporadaRepositorio>();
builder.Services.AddScoped<IHistorialPromocionTemporadaRepositorio, HistorialPromocionTemporadaRepositorio>();
builder.Services.AddScoped<IHitoCompraRepositorio, HitoCompraRepositorio>();
builder.Services.AddScoped<IHistorialHitoCompraRepositorio, HistorialHitoCompraRepositorio>();
builder.Services.AddScoped<IReferidosConfigRepositorio, ReferidosConfigRepositorio>();
builder.Services.AddScoped<IHistorialReferidoRepositorio, HistorialReferidoRepositorio>();
builder.Services.AddScoped<IReglaBasePuntosRepositorio, ReglaBasePuntosRepositorio>();
builder.Services.AddScoped<IAceleradorPuntosRepositorio, AceleradorPuntosRepositorio>();
builder.Services.AddScoped<IHistorialPuntosRepositorio, HistorialPuntosRepositorio>();
builder.Services.AddScoped<IConfiguracionQrRepositorio, ConfiguracionQrRepositorio>();
builder.Services.AddScoped<ICodigoSiatRepositorio, CodigoSiatRepositorio>();
builder.Services.AddScoped<IPuntosService, PuntosService>();

///////////////
builder.Services.AddScoped<CajaAbiertaFilter>();
builder.Services.AddScoped<IKafeYanaNotificador, KafeYanaNotificador>();
builder.Services.AddScoped<StockPayloadService>();
builder.Services.AddScoped<YanaBotService>();

builder.Services.AddEndpointsApiExplorer();

//Serviciio facturacion 
builder.Services.AddInfrastructure(builder.Configuration);

//Pra scalar
builder.Services.AddHttpContextAccessor();
 
builder.Services.AddHttpClient<YanaBotService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(30);
}); 

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapGraphQL();
app.MapScalarApiReference(options =>
{
    options.Title = "KafeYana API";
    options.Theme = ScalarTheme.DeepSpace;
});

app.UseExceptionHandler( _ => { });

app.UseHttpsRedirection();

app.UseCors("CorsPoliticy");

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<VerificarBloqueoMiddleware>();

app.MapControllers();
app.MapHub<KafeYanaHub>("/hubs/kafayana");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    if (!await roleManager.RoleExistsAsync(RolesKafe.Admin))
        await roleManager.CreateAsync(new IdentityRole<Guid>(RolesKafe.Admin));

    if (!await roleManager.RoleExistsAsync(RolesKafe.Mesero))
        await roleManager.CreateAsync(new IdentityRole<Guid>(RolesKafe.Mesero));

    if (!await roleManager.RoleExistsAsync(RolesKafe.Cajero))
        await roleManager.CreateAsync(new IdentityRole<Guid>(RolesKafe.Cajero));

    if (!await roleManager.RoleExistsAsync(RolesKafe.Asistente))
        await roleManager.CreateAsync(new IdentityRole<Guid>(RolesKafe.Asistente));

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await KafeYana.Infrastructure.Data.Seeders.AceleradorPuntosSeeder.SeedAsync(db);
    await KafeYana.Infrastructure.Data.Seeders.ReferidosConfigSeeder.SeedAsync(db);
    await KafeYana.Infrastructure.Data.Seeders.PuntoVentaSiatSeeder.SeedAsync(db);
}

app.Run();
    
