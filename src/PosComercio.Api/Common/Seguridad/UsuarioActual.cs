using System.Security.Claims;
using PosComercio.Api.Common.Tenancy;

namespace PosComercio.Api.Common.Seguridad;

public interface IUsuarioActual
{
    int UsuarioId { get; }
    string Nombre { get; }
    string Rol { get; }
    bool EsDueno { get; }
}

/// <summary>
/// Lee los datos del usuario autenticado desde su token.
/// Implementa ITenantContext: el NegocioId ahora sale del token, no de la configuración.
/// </summary>
public sealed class UsuarioActual(IHttpContextAccessor accessor) : IUsuarioActual, ITenantContext
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("No hay una petición HTTP activa.");

    public int UsuarioId => LeerEntero(ClaimsPos.UsuarioId);
    public int NegocioId => LeerEntero(ClaimsPos.NegocioId);
    public string Nombre => Principal.FindFirst(ClaimsPos.Nombre)?.Value ?? string.Empty;
    public string Rol => Principal.FindFirst(ClaimsPos.Rol)?.Value ?? string.Empty;
    public bool EsDueno => Rol == Roles.Dueno;

    private int LeerEntero(string tipoClaim) =>
        int.TryParse(Principal.FindFirst(tipoClaim)?.Value, out var valor)
            ? valor
            : throw new InvalidOperationException(
                $"El token no tiene el claim '{tipoClaim}'. ¿El endpoint permite acceso anónimo por error?");
}