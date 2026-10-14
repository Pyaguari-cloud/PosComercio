using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Data;

/// <summary>
/// Utilidades SOLO para el entorno de desarrollo. Nunca se ejecutan en producción.
/// </summary>
public static class DatosDesarrollo
{
    public const string PasswordDemo = "Demo123*";

    public static async Task AsignarPasswordsDemoAsync(IServiceProvider services)
    {
        // El DbContext es Scoped (vive una petición). Fuera de una petición,
        // creamos un "scope" manual para poder usarlo.
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosComercioDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var pendientes = await db.Usuarios
            .Where(u => u.PasswordHash == "PENDIENTE_HASH")
            .ToListAsync();

        foreach (var usuario in pendientes)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, PasswordDemo);
        }

        if (pendientes.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }
}