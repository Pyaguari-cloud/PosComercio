using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Usuarios;

namespace PosComercio.Api.Services.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioListaDto>> ListarActivosAsync(CancellationToken ct);
    Task<Resultado<UsuarioListaDto>> AsignarPinAsync(int usuarioId, string pin, CancellationToken ct);
}

public sealed class UsuarioService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IPasswordHasher<Usuario> hasher,
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
}