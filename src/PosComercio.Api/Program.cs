using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Services.Auth;
using PosComercio.Api.Services.Productos;
using PosComercio.Api.Services.Usuarios;
using PosComercio.Api.Data.Interceptores;
using PosComercio.Api.Services.Caja;
using PosComercio.Api.Services.Negocios;
using PosComercio.Api.Services.Autorizacion;
using PosComercio.Api.Services.Ventas;
using PosComercio.Api.Services.Catalogo;
using PosComercio.Api.Services.Inventario;

var builder = WebApplication.CreateBuilder(args);

// ---------- Infraestructura ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull); builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<SesionNegocioInterceptor>();

builder.Services.AddDbContext<PosComercioDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PosComercio"))
           .AddInterceptors(serviceProvider.GetRequiredService<SesionNegocioInterceptor>()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PosComercioDbContext>("base-de-datos");

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(Politicas.SoloDueno, policy => policy.RequireRole(Roles.Dueno));


// ---------- Autenticación (JWT) ----------
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Seccion)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configura la validación del token usando JwtOptions ya validado
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        options.MapInboundClaims = false; // conserva los nombres "sub", "role", "negocio_id" tal cual
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimsPos.Nombre,
            RoleClaimType = ClaimsPos.Rol
        };
    });

// Seguro por defecto: TODO endpoint exige estar autenticado, salvo los marcados [AllowAnonymous]
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ---------- Límite de intentos de login ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(PoliticasRateLimit.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // PIN: 5 intentos por minuto, por negocio + dispositivo (IP)
    options.AddPolicy(PoliticasRateLimit.Pin, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{httpContext.User.FindFirst(ClaimsPos.NegocioId)?.Value}:{httpContext.Connection.RemoteIpAddress}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- Servicios de la aplicación ----------
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Una sola instancia de UsuarioActual por petición, expuesta con dos interfaces
builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddScoped<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioActual>());
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<UsuarioActual>());

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddScoped<IConfiguracionNegocioService, ConfiguracionNegocioService>();
builder.Services.AddScoped<ICajaService, CajaService>();

builder.Services.AddScoped<IAutorizacionDuenoService, AutorizacionDuenoService>();
builder.Services.AddScoped<IVentaService, VentaService>();

builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IProductoEdicionService, ProductoEdicionService>();

builder.Services.AddScoped<IKardexService, KardexService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();

var app = builder.Build();

// ---------- Datos de desarrollo ----------
if (app.Environment.IsDevelopment())
{
    await DatosDesarrollo.AsignarPasswordsDemoAsync(app.Services);
}

// ---------- Pipeline HTTP (el orden importa) ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // 1. ¿Quién eres? (lee y valida el token)
app.UseAuthorization();    // 2. ¿Puedes entrar aquí?
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();