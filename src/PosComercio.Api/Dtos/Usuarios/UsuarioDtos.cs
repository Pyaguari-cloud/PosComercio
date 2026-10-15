using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Usuarios;

/// <summary>Para la pantalla de "¿quién va a vender?". No expone datos sensibles.</summary>
public sealed record UsuarioListaDto(
    int UsuarioId,
    string Nombre,
    string Rol,
    bool TienePin);

public sealed record AsignarPinRequest(
    [Required, RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener exactamente 4 dígitos.")]
    string Pin);

/// <summary>Vista completa para la pantalla de administración de usuarios (solo el dueño).</summary>
public sealed record UsuarioGestionDto(
    int UsuarioId,
    string Nombre,
    string UsuarioLogin,
    string Rol,
    bool Activo,
    bool TienePin,
    DateTime FechaCreacionUtc);

public sealed record CrearUsuarioRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, StringLength(80, MinimumLength = 3)] string UsuarioLogin,
    [Required, StringLength(100)] string Password,
    [Required, RegularExpression("^(Dueno|Vendedor)$", ErrorMessage = "El rol debe ser Dueno o Vendedor.")]
    string Rol,
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener exactamente 4 dígitos.")]
    string? Pin);

public sealed record ActualizarUsuarioRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, RegularExpression("^(Dueno|Vendedor)$", ErrorMessage = "El rol debe ser Dueno o Vendedor.")]
    string Rol,
    bool Activo);

public sealed record RestablecerPasswordRequest(
    [Required, StringLength(100)] string PasswordNueva);