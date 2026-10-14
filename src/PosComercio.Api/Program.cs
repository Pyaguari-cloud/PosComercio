using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de servicios ----------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PosComercioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PosComercio")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PosComercioDbContext>("base-de-datos");

var app = builder.Build();

// ---------- Pipeline HTTP ----------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();