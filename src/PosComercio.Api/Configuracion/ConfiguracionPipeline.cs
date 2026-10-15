using Microsoft.Extensions.FileProviders;
using PosComercio.Api.Common.Almacenamiento;
using PosComercio.Api.Data;
using Scalar.AspNetCore;

namespace PosComercio.Api.Configuracion;

/// <summary>Datos iniciales y orden de los middlewares.</summary>
public static class ConfiguracionPipeline
{
    public static async Task InicializarDatosAsync(this WebApplication app)
    {
        // En todos los entornos: crea el AdminSaaS inicial si está configurado y no existe
        await InicializadorAdmin.CrearSiNoExisteAsync(app.Services);

        if (app.Environment.IsDevelopment())
        {
            await DatosDesarrollo.AsignarPasswordsDemoAsync(app.Services);
        }
    }

    /// <summary>
    /// El ORDEN de estas líneas importa: cada middleware decide si atiende la petición
    /// o la pasa al siguiente.
    /// </summary>
    public static WebApplication UsarPipelinePos(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        app.UseHttpsRedirection();
        app.UsarFotosPublicas();               // antes de la autenticación: las <img> no envían token
        app.UseCors(ServiciosInfraestructura.PoliticaCors);   // antes de la autenticación: el preflight no trae token

        app.UseAuthentication();               // 1. ¿Quién eres?
        app.UseAuthorization();                // 2. ¿Puedes entrar aquí?
        app.UseRateLimiter();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();

        return app;
    }

    private static void UsarFotosPublicas(this WebApplication app)
    {
        var ruta = Path.Combine(app.Environment.ContentRootPath, AlmacenamientoLocal.CarpetaRaiz);
        Directory.CreateDirectory(ruta);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(ruta),
            RequestPath = AlmacenamientoLocal.RutaPublica,
            // Cada foto tiene un nombre único que nunca cambia: el navegador puede guardarla un año
            OnPrepareResponse = contexto =>
                contexto.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
        });
    }
}