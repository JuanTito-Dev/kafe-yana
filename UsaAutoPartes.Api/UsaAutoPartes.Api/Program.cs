using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Net;
using System.Text;
using UsaAutoPartes.Api.Handlers;
using UsaAutoPartes.Api.Schema.Queries;
using UsaAutoPartes.Api.Schema.Types;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Application.IServicios;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Domain.Enum.CookieNames;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;
using UsaAutoPartes.Infrastructure.Data;
using UsaAutoPartes.Infrastructure.Data.Repositorio;
using UsaAutoPartes.Infrastructure.Servicios.Processors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.JwtOptionsSecction));

builder.Services.AddDataProtection();

builder.Services.AddIdentityCore<Usuario>(opt =>
{
    opt.Password.RequireDigit = false;
    opt.Password.RequireLowercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequiredLength = 6;
    opt.User.RequireUniqueEmail = true;
}).AddRoles<IdentityRole<Guid>>()
  .AddEntityFrameworkStores<AppDbContext>()
  .AddSignInManager()
  .AddDefaultTokenProviders();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration["ConexionDataBase:CadenaConexion"]);
} );

builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessor>();
builder.Services.AddScoped<IAuthenticationRepositorio, AuthenticationRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IUnitWork, UnitWork>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
builder.Services.AddScoped<IImportacionRepositorio, ImportacionRepositorio>();


builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtoptions = builder.Configuration.GetSection(JwtOptions.JwtOptionsSecction)
    .Get<JwtOptions>() ?? throw new ArgumentException(nameof(JwtOptions));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtoptions.Issuer,
        ValidAudience = jwtoptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtoptions.SecretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accesstoken = context.Request.Cookies[CookiesNames.access.ToString()];

            if (!string.IsNullOrEmpty(accesstoken))
            {
                context.Token = accesstoken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddMapster();
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<AuthenticationHandler>();

//CORS

var origins = builder.Configuration.GetSection("cors:origins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(X =>
{
    X.AddPolicy("CorsPoliticy", polity =>
    {
        polity.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
        
});

builder.Services.AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddTypeExtension<ProductoQuery>()
    .AddTypeExtension<ProveedorQuery>()
    .AddTypeExtension<ImportacionQuery>()
    .AddType<ProductoType>()
    .AddType<MeQuery>()
    .AddType<ProveedorType>()
    .AddType<ImportacionType>()
    .AddAuthorization()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapOpenApi();
app.MapGraphQL();
app.MapScalarApiReference(options =>
{
    options.Title = "UsaAutoPartes API";
    options.Theme = ScalarTheme.DeepSpace;
}
); 

app.UseExceptionHandler(_ => { });
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    if (!await roleManager.RoleExistsAsync(UsuarioRoles.Admin.ToString()))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>(UsuarioRoles.Admin.ToString()));
    }
}

app.Run();

