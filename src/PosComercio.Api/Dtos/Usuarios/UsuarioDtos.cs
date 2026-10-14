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