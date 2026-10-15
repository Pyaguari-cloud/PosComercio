using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Dtos.Auth;
using PosComercio.Api.Services.Auth;

namespace PosComercio.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IUsuarioActual usuarioActual,
    ITenantContext tenant) : PosControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasRateLimit.Login)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var resultado = await authService.LoginAsync(request, ct);
        return Responder(resultado);
    }

    /// <summary>
    /// Cambia de usuario en el mismo dispositivo usando el PIN.
    /// Requiere un token válido de alguien del mismo negocio.
    /// </summary>
    [HttpPost("cambiar-usuario")]
    [EnableRateLimiting(PoliticasRateLimit.Pin)]
    public async Task<ActionResult<LoginResponse>> CambiarUsuario(CambiarUsuarioRequest request, CancellationToken ct)
    {
        var resultado = await authService.CambiarUsuarioPorPinAsync(request, ct);
        return Responder(resultado);
    }

    /// <summary>Devuelve los datos del usuario dueño del token. Útil para probar.</summary>
    [HttpGet("yo")]
    public ActionResult<SesionActualDto> Yo() =>
        Ok(new SesionActualDto(usuarioActual.UsuarioId, usuarioActual.Nombre, usuarioActual.Rol, tenant.NegocioId));

    /// <summary>Cambia la contraseña del usuario actual.</summary>
    [HttpPost("cambiar-password")]
    [EnableRateLimiting(PoliticasRateLimit.Login)]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct)
    {
        var error = await authService.CambiarPasswordAsync(request, ct);
        return error is null ? NoContent() : Problema(error);
    }
}