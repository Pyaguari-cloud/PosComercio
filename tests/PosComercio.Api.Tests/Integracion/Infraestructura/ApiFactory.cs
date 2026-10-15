using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PosComercio.Api.Tests.Integracion.Infraestructura;

/// <summary>
/// Arranca la API completa en memoria, conectada a la base de pruebas.
/// Se crea UNA vez y la comparten todas las pruebas de integración.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Antes de arrancar la API: base de datos limpia
    public Task InitializeAsync() => BaseDeDatosPruebas.RecrearAsync();

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Un entorno propio: NO carga tus User Secrets ni los datos de desarrollo
        builder.UseEnvironment("Pruebas");

        builder.ConfigureAppConfiguration(configuracion =>
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PosComercio"] = ConfiguracionPruebas.CadenaConexion,
                ["Jwt:Key"] = "clave-que-solo-existe-en-las-pruebas-0123456789-abcdefghij",
                ["Limites:IntentosPorMinuto"] = "1000"
            }));
    }
}