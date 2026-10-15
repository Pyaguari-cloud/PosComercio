using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Services.Usuarios;

namespace PosComercio.Api.Data;

/// <summary>
/// Crea el primer usuario AdminSaaS al arrancar, SOLO si está configurado y no existe todavía.
/// Funciona en cualquier entorno (también en producción).
/// </summary>
public static class InicializadorAdmin
{
    public static async Task CrearSiNoExisteAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var proveedor = scope.ServiceProvider;

        var configuracion = proveedor.GetRequiredService<IConfiguration>();
        var loginConfigurado = configuracion["AdminInicial:UsuarioLogin"];
        var passwordConfigurada = configuracion["AdminInicial:Password"];

        if (string.IsNullOrWhiteSpace(loginConfigurado) || string.IsNullOrWhiteSpace(passwordConfigurada))
        {
            return; // no configurado: no hay nada que hacer
        }

        var db = proveedor.GetRequiredService<PosComercioDbContext>();
        var login = Logins.Normalizar(loginConfigurado);

        if (await db.Usuarios.AnyAsync(u => u.UsuarioLogin == login))
        {
            return; // ya existe: no tocar nada
        }

        if (ReglasPassword.Validar(passwordConfigurada) is not null)
        {
            throw new InvalidOperationException(
                "La contraseña de 'AdminInicial' no cumple las reglas: mínimo 8 caracteres, con letras y números.");
        }

        var hasher = proveedor.GetRequiredService<IPasswordHasher<Usuario>>();
        var ahora = proveedor.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InicializadorAdmin));

        await using var transaccion = await db.Database.BeginTransactionAsync();

        var negocioInterno = new Negocio
        {
            NombreComercial = "Administración SaaS",
            TipoNegocio = "Interno",
            Moneda = "USD",
            ZonaHoraria = "America/Guayaquil",
            PlanActual = "PRO",
            Activo = true,
            FechaCreacion = ahora
        };

        db.Negocios.Add(negocioInterno);
        await db.SaveChangesAsync();

        var admin = new Usuario
        {
            NegocioId = negocioInterno.NegocioId,
            Nombre = "Administrador",
            UsuarioLogin = login,
            PasswordHash = string.Empty,
            Rol = Roles.AdminSaaS,
            Activo = true,
            FechaCreacion = ahora
        };
        admin.PasswordHash = hasher.HashPassword(admin, passwordConfigurada);

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        logger.LogWarning(
            "Usuario AdminSaaS '{Login}' creado. Por seguridad, elimina 'AdminInicial:Password' de la configuración.",
            login);
    }
}