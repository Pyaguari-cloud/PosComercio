using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Tenancy;          // NUEVO
using PosComercio.Api.Data;
using PosComercio.Api.Services.Productos;      // NUEVO

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de servicios ----------
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();          // NUEVO: errores en formato estándar

builder.Services.AddDbContext<PosComercioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PosComercio")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PosComercioDbContext>("base-de-datos");

// NUEVO: servicios de la aplicación
builder.Services.AddScoped<ITenantContext, DesarrolloTenantContext>();
builder.Services.AddScoped<IProductoService, ProductoService>();

var app = builder.Build();

// ---------- Pipeline HTTP ----------
app.UseExceptionHandler();                     // NUEVO: captura errores no controlados
app.UseStatusCodePages();                      // NUEVO: 404/401 también con formato estándar

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();