using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Common.Seguridad;

/// <summary>
/// Configuración del JWT, leída de la sección "Jwt" (appsettings + user secrets).
/// Se valida al arrancar: si falta algo, la API no inicia.
/// </summary>
public sealed class JwtOptions
{
    public const string Seccion = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Range(5, 1440)]
    public int ExpiracionMinutos { get; init; } = 720;
}