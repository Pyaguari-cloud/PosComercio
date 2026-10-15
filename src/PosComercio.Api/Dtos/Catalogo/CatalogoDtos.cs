using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Catalogo;

public sealed record CategoriaDto(int CategoriaId, string Nombre, string ColorHex);

public sealed record CrearCategoriaRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener el formato #RRGGBB.")]
    string ColorHex);

public sealed record UbicacionDto(int UbicacionId, string Nombre, string? Descripcion);

public sealed record CrearUbicacionRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [StringLength(250)] string? Descripcion);