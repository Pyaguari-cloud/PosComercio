using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Auth;

namespace PosComercio.Api.Services.Auth;

public interface IAuthService
{
    Task<Resultado<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Resultado<LoginResponse>> CambiarUsuarioPorPinAsync(CambiarUsuarioRequest request, CancellationToken ct);
}

public sealed class AuthService(
    PosComercioDbContext db,
    ITokenService tokenService,
    IPasswordHasher<Usuario> hasher,
    ITenantContext tenant,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly string HashFicticio =
        new PasswordHasher<Usuario>().HashPassword(new Usuario(), "contrasena-ficticia-para-igualar-tiempos");

    public async Task<Resultado<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var login = request.UsuarioLogin.Trim().ToLowerInvariant();

        var usuario = await db.Usuarios
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u => u.UsuarioLogin == login, ct);

        if (usuario is null || !usuario.Activo)
        {
            hasher.VerifyHashedPassword(new Usuario(), HashFicticio, request.Password);
            logger.LogWarning("Login fallido: usuario inexistente o inactivo ({Login})", login);
            return ErroresAuth.CredencialesInvalidas;
        }

        var verificacion = VerificarHash(usuario, usuario.PasswordHash, request.Password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login fallido: contraseña incorrecta para el usuario {UsuarioId}", usuario.UsuarioId);
            return ErroresAuth.CredencialesInvalidas;
        }

        if (!usuario.Negocio.Activo || usuario.Negocio.PlanActual == "SUSPENDIDO")
        {
            return ErroresAuth.NegocioSuspendido;
        }

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Login exitoso: usuario {UsuarioId}, negocio {NegocioId}",
            usuario.UsuarioId, usuario.NegocioId);

        return CrearRespuesta(usuario);
    }

    public async Task<Resultado<LoginResponse>> CambiarUsuarioPorPinAsync(
        CambiarUsuarioRequest request, CancellationToken ct)
    {
        // Solo usuarios del MISMO negocio que el token actual
        var usuario = await db.Usuarios
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u =>
                u.UsuarioId == request.UsuarioId &&
                u.NegocioId == tenant.NegocioId &&
                u.Activo, ct);

        if (usuario is null)
        {
            return ErroresAuth.UsuarioNoDisponible;
        }

        if (usuario.PinHash is null)
        {
            return ErroresAuth.PinNoAsignado;
        }

        if (VerificarHash(usuario, usuario.PinHash, request.Pin) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("PIN incorrecto para el usuario {UsuarioId} del negocio {NegocioId}",
                usuario.UsuarioId, usuario.NegocioId);
            return ErroresAuth.PinIncorrecto;
        }

        logger.LogInformation("Cambio de usuario por PIN: ahora {UsuarioId}, negocio {NegocioId}",
            usuario.UsuarioId, usuario.NegocioId);

        return CrearRespuesta(usuario);
    }

    private LoginResponse CrearRespuesta(Usuario usuario)
    {
        var token = tokenService.Generar(usuario);

        return new LoginResponse(
            token.AccessToken,
            token.ExpiraEnUtc,
            new UsuarioSesionDto(
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Rol,
                usuario.NegocioId,
                usuario.Negocio.NombreComercial));
    }

    private PasswordVerificationResult VerificarHash(Usuario usuario, string hashGuardado, string valor)
    {
        try
        {
            return hasher.VerifyHashedPassword(usuario, hashGuardado, valor);
        }
        catch (FormatException)
        {
            logger.LogError("El usuario {UsuarioId} tiene un hash con formato inválido", usuario.UsuarioId);
            return PasswordVerificationResult.Failed;
        }
    }
}