using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Usuarios;
using PosComercio.Api.Common.Seguridad;

namespace PosComercio.Api.Services.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioListaDto>> ListarActivosAsync(CancellationToken ct);
    Task<Resultado<UsuarioListaDto>> AsignarPinAsync(int usuarioId, string pin, CancellationToken ct);

    // NUEVOS: gestión (solo el dueño)
    Task<IReadOnlyList<UsuarioGestionDto>> ListarParaGestionAsync(CancellationToken ct);
    Task<Resultado<UsuarioGestionDto>> CrearAsync(CrearUsuarioRequest request, CancellationToken ct);
    Task<Resultado<UsuarioGestionDto>> ActualizarAsync(int usuarioId, ActualizarUsuarioRequest request, CancellationToken ct);
    Task<Resultado<UsuarioGestionDto>> RestablecerPasswordAsync(int usuarioId, string passwordNueva, CancellationToken ct);

}

public sealed class UsuarioService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuarioActual,
    IPasswordHasher<Usuario> hasher,
    IValidadorSesion validadorSesion,
    TimeProvider tiempo,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioListaDto>> ListarActivosAsync(CancellationToken ct) =>
        await db.Usuarios
            .AsNoTracking()
            .Where(u => u.NegocioId == tenant.NegocioId && u.Activo)
            .OrderBy(u => u.Nombre)
            .Select(u => new UsuarioListaDto(u.UsuarioId, u.Nombre, u.Rol, u.PinHash != null))
            .ToListAsync(ct);

    public async Task<Resultado<UsuarioListaDto>> AsignarPinAsync(int usuarioId, string pin, CancellationToken ct)
    {
        if (ReglasPin.EsDebil(pin))
        {
            return ErroresUsuarios.PinDebil;
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(
            u => u.UsuarioId == usuarioId && u.NegocioId == tenant.NegocioId && u.Activo, ct);

        if (usuario is null)
        {
            return ErroresUsuarios.NoEncontrado;
        }

        // El PIN se guarda hasheado, igual que la contraseña
        usuario.PinHash = hasher.HashPassword(usuario, pin);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("PIN asignado al usuario {UsuarioId} del negocio {NegocioId}",
            usuario.UsuarioId, usuario.NegocioId);

        return new UsuarioListaDto(usuario.UsuarioId, usuario.Nombre, usuario.Rol, TienePin: true);
    }

    // =====================================================================
    // GESTIÓN DE USUARIOS (solo el dueño)
    // =====================================================================

    public async Task<IReadOnlyList<UsuarioGestionDto>> ListarParaGestionAsync(CancellationToken ct) =>
        await db.Usuarios
            .AsNoTracking()
            .Where(u => u.NegocioId == tenant.NegocioId)
            .OrderByDescending(u => u.Activo)           // activos primero
            .ThenBy(u => u.Nombre)
            .Select(u => new UsuarioGestionDto(
                u.UsuarioId, u.Nombre, u.UsuarioLogin, u.Rol, u.Activo, u.PinHash != null, u.FechaCreacion))
            .ToListAsync(ct);

    public async Task<Resultado<UsuarioGestionDto>> CrearAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        if (ReglasPassword.Validar(request.Password) is { } errorPassword)
        {
            return errorPassword;
        }

        if (request.Pin is { } pinNuevo && ReglasPin.EsDebil(pinNuevo))
        {
            return ErroresUsuarios.PinDebil;
        }

        var login = Logins.Normalizar(request.UsuarioLogin);

        if (await db.Usuarios.AnyAsync(u => u.UsuarioLogin == login, ct))
        {
            return ErroresUsuarios.LoginEnUso;
        }

        var usuario = new Usuario
        {
            NegocioId = tenant.NegocioId,
            Nombre = request.Nombre.Trim(),
            UsuarioLogin = login,
            PasswordHash = string.Empty,       // se asigna en la línea siguiente
            Rol = request.Rol,
            Activo = true,
            FechaCreacion = tiempo.GetUtcNow().UtcDateTime
        };

        usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);

        if (request.Pin is { } pin)
        {
            usuario.PinHash = hasher.HashPassword(usuario, pin);
        }

        db.Usuarios.Add(usuario);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            // Alguien registró el mismo login justo ahora
            return ErroresUsuarios.LoginEnUso;
        }

        logger.LogInformation("Usuario {NuevoId} ({Rol}) creado en el negocio {NegocioId} por {UsuarioId}",
            usuario.UsuarioId, usuario.Rol, tenant.NegocioId, usuarioActual.UsuarioId);

        return AGestionDto(usuario);
    }

    public async Task<Resultado<UsuarioGestionDto>> ActualizarAsync(
        int usuarioId, ActualizarUsuarioRequest request, CancellationToken ct)
    {
        // Sin filtro por Activo: el dueño también puede editar (y reactivar) usuarios inactivos
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u =>
            u.NegocioId == tenant.NegocioId && u.UsuarioId == usuarioId, ct);

        if (usuario is null)
        {
            return ErroresUsuarios.NoEncontrado;
        }

        var esUnoMismo = usuario.UsuarioId == usuarioActual.UsuarioId;
        if (esUnoMismo && (!request.Activo || request.Rol != Roles.Dueno))
        {
            return ErroresUsuarios.NoPuedesQuitarteAcceso;
        }

        // Defensa adicional: nunca dejar el negocio sin dueños activos
        var dejaDeSerDuenoActivo = usuario.Rol == Roles.Dueno && usuario.Activo
                                && (request.Rol != Roles.Dueno || !request.Activo);
        if (dejaDeSerDuenoActivo)
        {
            var otrosDuenosActivos = await db.Usuarios.CountAsync(u =>
                u.NegocioId == tenant.NegocioId && u.UsuarioId != usuarioId
                && u.Rol == Roles.Dueno && u.Activo, ct);

            if (otrosDuenosActivos == 0)
            {
                return ErroresUsuarios.UltimoDueno;
            }
        }

        usuario.Nombre = request.Nombre.Trim();
        usuario.Rol = request.Rol;
        usuario.Activo = request.Activo;

        await db.SaveChangesAsync(ct);

        // Si lo desactivamos o le cambiamos el rol, que se aplique ya
        validadorSesion.Invalidar(usuario.UsuarioId);

        logger.LogInformation("Usuario {UsuarioEditado} actualizado por {UsuarioId}: rol {Rol}, activo {Activo}",
            usuario.UsuarioId, usuarioActual.UsuarioId, usuario.Rol, usuario.Activo);

        return AGestionDto(usuario);
    }

    public async Task<Resultado<UsuarioGestionDto>> RestablecerPasswordAsync(
        int usuarioId, string passwordNueva, CancellationToken ct)
    {
        if (ReglasPassword.Validar(passwordNueva) is { } errorPassword)
        {
            return errorPassword;
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u =>
            u.NegocioId == tenant.NegocioId && u.UsuarioId == usuarioId, ct);

        if (usuario is null)
        {
            return ErroresUsuarios.NoEncontrado;
        }

        usuario.PasswordHash = hasher.HashPassword(usuario, passwordNueva);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Contraseña del usuario {UsuarioEditado} restablecida por {UsuarioId}",
            usuario.UsuarioId, usuarioActual.UsuarioId);

        return AGestionDto(usuario);
    }

    private static UsuarioGestionDto AGestionDto(Usuario u) => new(
        u.UsuarioId, u.Nombre, u.UsuarioLogin, u.Rol, u.Activo, u.PinHash is not null, u.FechaCreacion);
}