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