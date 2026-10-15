using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Services.Autorizacion;

public static class ErroresAutorizacion
{
    public static readonly Error Requerida = new(
        "autorizacion.requerida",
        "Esta operación necesita la autorización del dueño. Pídele que ingrese su PIN.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PinIncorrecto = new(
        "autorizacion.pin_incorrecto",
        "El PIN del dueño es incorrecto.",
        StatusCodes.Status403Forbidden);

    public static readonly Error DemasiadosIntentos = new(
        "autorizacion.demasiados_intentos",
        "Demasiados intentos fallidos. Espera unos minutos.",
        StatusCodes.Status429TooManyRequests);
}

public interface IAutorizacionDuenoService
{
    /// <summary>
    /// Devuelve el UsuarioId del dueño que autoriza.
    /// Si el usuario actual ES dueño, se autoriza a sí mismo sin PIN.
    /// </summary>
    Task<Resultado<int>> AutorizarAsync(string? pinDueno, CancellationToken ct);
}

public sealed class AutorizacionDuenoService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IPasswordHasher<Usuario> hasher,
    IMemoryCache cache,
    ILogger<AutorizacionDuenoService> logger) : IAutorizacionDuenoService
{
    private const int MaximoFallos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);

    public async Task<Resultado<int>> AutorizarAsync(string? pinDueno, CancellationToken ct)
    {
        if (usuario.EsDueno)
        {
            return usuario.UsuarioId;
        }

        if (string.IsNullOrEmpty(pinDueno))
        {
            return ErroresAutorizacion.Requerida;
        }

        // Contador de fallos por negocio, en memoria del servidor
        var claveFallos = $"fallos-pin-dueno:{tenant.NegocioId}";
        var fallos = cache.TryGetValue(claveFallos, out int valor) ? valor : 0;

        if (fallos >= MaximoFallos)
        {
            return ErroresAutorizacion.DemasiadosIntentos;
        }

        var duenos = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.NegocioId == tenant.NegocioId
                     && u.Activo
                     && u.Rol == Roles.Dueno
                     && u.PinHash != null)
            .ToListAsync(ct);

        foreach (var dueno in duenos)
        {
            if (hasher.VerificarSeguro(dueno, dueno.PinHash!, pinDueno) != PasswordVerificationResult.Failed)
            {
                cache.Remove(claveFallos);
                logger.LogInformation("Dueño {DuenoId} autorizó una operación del usuario {UsuarioId}",
                    dueno.UsuarioId, usuario.UsuarioId);
                return dueno.UsuarioId;
            }
        }

        cache.Set(claveFallos, fallos + 1, DuracionBloqueo);
        logger.LogWarning("PIN de dueño incorrecto, intento {Intento}, usuario {UsuarioId}, negocio {NegocioId}",
            fallos + 1, usuario.UsuarioId, tenant.NegocioId);

        return ErroresAutorizacion.PinIncorrecto;
    }
}