using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Almacenamiento;
using PosComercio.Api.Common.OpenApi;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Interceptores;
using PosComercio.Api.Services.Fotos;

namespace PosComercio.Api.Configuracion;

/// <summary>Piezas técnicas: JSON, base de datos, caché, CORS, archivos, documentación.</summary>
public static class ServiciosInfraestructura
{
    public const string PoliticaCors = "Frontend";

    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        services.AddOpenApi(options => options.AddDocumentTransformer<TransformadorSeguridadBearer>());
        services.AddProblemDetails();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddCorsFrontend(configuration);
        services.AddBaseDeDatos(configuration);

        services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocal>();
        services.AddSingleton<IProcesadorImagenes, ProcesadorImagenes>();

        return services;
    }

    private static void AddCorsFrontend(this IServiceCollection services, IConfiguration configuration)
    {
        var origenesPermitidos = configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PoliticaCors, politica => politica
            .WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location")));
    }

    private static void AddBaseDeDatos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SesionNegocioInterceptor>();

        services.AddDbContext<PosComercioDbContext>((serviceProvider, options) =>
            options.UseSqlServer(configuration.GetConnectionString("PosComercio"))
                   .AddInterceptors(serviceProvider.GetRequiredService<SesionNegocioInterceptor>()));

        services.AddHealthChecks()
            .AddDbContextCheck<PosComercioDbContext>("base-de-datos");
    }
}