using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Configuracion;

/// <summary>Autenticación JWT, autorización, límites de peticiones y usuario actual.</summary>
public static class ServiciosSeguridad
{
    public static IServiceCollection AddSeguridad(this IServiceCollection services)
    {
        services.AddJwt();
        services.AddAutorizacion();
        services.AddLimitesDePeticiones();

        services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IValidadorSesion, ValidadorSesion>();

        // Una sola instancia de UsuarioActual por petición, expuesta con dos interfaces
        services.AddScoped<UsuarioActual>();
        services.AddScoped<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioActual>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<UsuarioActual>());

        return services;
    }

    private static void AddJwt(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Seccion)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
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

                options.Events = new JwtBearerEvents
                {
                    // Después de validar firma y expiración: ¿el usuario y su negocio siguen activos?
                    OnTokenValidated = async contexto =>
                    {
                        var claimUsuario = contexto.Principal?.FindFirst(ClaimsPos.UsuarioId)?.Value;
                        var validador = contexto.HttpContext.RequestServices.GetRequiredService<IValidadorSesion>();

                        var esValida = int.TryParse(claimUsuario, out var usuarioId)
                                    && await validador.EsValidaAsync(usuarioId, contexto.HttpContext.RequestAborted);

                        if (!esValida)
                        {
                            contexto.Fail("El usuario o el negocio ya no están activos.");
                        }
                    }
                };
            });
    }

    private static void AddAutorizacion(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Seguro por defecto: todo exige estar autenticado, salvo [AllowAnonymous]
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Politicas.SoloDueno, policy => policy.RequireRole(Roles.Dueno))
            .AddPolicy(Politicas.SoloAdminSaaS, policy => policy.RequireRole(Roles.AdminSaaS));
    }

    private static void AddLimitesDePeticiones(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Login: 5 intentos por minuto por IP
            options.AddPolicy(PoliticasRateLimit.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    factory: _ => VentanaDeCincoPorMinuto()));

            // PIN: 5 intentos por minuto por negocio + dispositivo
            options.AddPolicy(PoliticasRateLimit.Pin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{httpContext.User.FindFirst(ClaimsPos.NegocioId)?.Value}:{httpContext.Connection.RemoteIpAddress}",
                    factory: _ => VentanaDeCincoPorMinuto()));
        });
    }

    private static FixedWindowRateLimiterOptions VentanaDeCincoPorMinuto() => new()
    {
        PermitLimit = 5,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    };
}