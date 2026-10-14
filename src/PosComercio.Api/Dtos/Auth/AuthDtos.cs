using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Auth;

public sealed record LoginRequest(
    [Required, StringLength(80)] string UsuarioLogin,
    [Required, StringLength(100)] string Password);

public sealed record UsuarioSesionDto(
    int UsuarioId,
    string Nombre,
    string Rol,
    int NegocioId,
    string NombreNegocio);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiraEnUtc,
    UsuarioSesionDto Usuario);

public sealed record SesionActualDto(
    int UsuarioId,
    string Nombre,
    string Rol,
    int NegocioId);