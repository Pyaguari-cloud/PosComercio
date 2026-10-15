using PosComercio.Api.Configuracion;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfraestructura(builder.Configuration)
    .AddSeguridad()
    .AddServiciosAplicacion();

var app = builder.Build();

await app.InicializarDatosAsync();
app.UsarPipelinePos();

app.Run();

/// <summary>
/// Hace pública la clase Program (generada por las instrucciones de nivel superior)
/// para que las pruebas de integración puedan arrancar la API.
/// </summary>
public partial class Program;