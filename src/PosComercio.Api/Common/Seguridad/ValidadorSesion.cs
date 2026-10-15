using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PosComercio.Api.Data;

namespace PosComercio.Api.Common.Seguridad;

public interface IValidadorSesion
{
    /// <summary>True si el usuario y su negocio siguen activos.</summary>
    Task<bool> EsValidaAsync(int usuarioId, CancellationToken ct);

    /// <summary>Olvida el resultado guardado, para que el próximo request vuelva a consultar.</summary>
    void Invalidar(int usuarioId);
}

public sealed class ValidadorSesion(PosComercioDbContext db, IMemoryCache cache) : IValidadorSesion
{
    private static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(60);

    public async Task<bool> EsValidaAsync(int usuarioId, CancellationToken ct) =>
        await cache.GetOrCreateAsync(Clave(usuarioId), async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = DuracionCache;

            return await db.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => u.Activo
                          && u.Negocio.Activo
                          && u.Negocio.PlanActual != "SUSPENDIDO")
                .FirstOrDefaultAsync(ct);
        });

    public void Invalidar(int usuarioId) => cache.Remove(Clave(usuarioId));

    private static string Clave(int usuarioId) => $"sesion-valida:{usuarioId}";
}