using KafeYana.Api.GraphQLMap;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Core.Entities.Entity;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Data.Repositorio;
using KafeYana.Infrastructure.Options;
using KafeYana.Infrastructure.Procesos;
using KafeYana.Infrastructure.Servicios;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

var builder = WebApplication.CreateBuilder(args);

//Servicios

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.JwtOptionsKey));

//Conexion Base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DataBase:Conexion"])
);

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
            Context.Token = Context.Request.Cookies["ACCESS_TOKEN"];
            return Task.CompletedTask;
        }
    };
});

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
    .AddQueryType(d => d.Name("Query"))
    .AddTypeExtension<ProductoQuery>()
    .AddTypeExtension<UsuarioQuery>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
    .AddFiltering()
    .AddSorting()
    .AddAuthorization();

//Servicios 
builder.Services.AddScoped<IAuthTokenProcesador, AuthTokenProcesador>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped(typeof(IGenericRepositorio<>), typeof(GenericRepositorio<>));
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();

builder.Services.AddEndpointsApiExplorer();

//Pra scalar
builder.Services.AddHttpContextAccessor();

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

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
    }
}

app.Run();
