using System.ComponentModel.DataAnnotations;
namespace PosComercio.Api.Common.Seguridad;

/// <summary>Nombres de los datos (claims) que viajan dentro del token.</summary>
public static class ClaimsPos
{
    public const string UsuarioId = "sub";
    public const string NegocioId = "negocio_id";
    public const string Nombre = "name";
    public const string Rol = "role";
}

/// <summary>Roles válidos. Coinciden con el CHECK de la tabla Usuarios.</summary>
public static class Roles
{
    public const string AdminSaaS = "AdminSaaS";
    public const string Dueno = "Dueno";
    public const string Vendedor = "Vendedor";
}

/// <summary>Nombres de las políticas de límite de peticiones.</summary>
public static class PoliticasRateLimit
{
    public const string Login = "login";
    public const string Pin = "pin";
}

/// <summary>Políticas de autorización (quién puede hacer qué).</summary>
public static class Politicas
{
    public const string SoloDueno = "SoloDueno";
    public const string SoloAdminSaaS = "SoloAdminSaaS";
}

public static class Logins
{
    /// <summary>"  Juan@Correo.com " → "juan@correo.com"</summary>
    public static string Normalizar(string login) => login.Trim().ToLowerInvariant();
}

/// <summary>Límites de intentos por minuto (login y PIN). Configurable por entorno.</summary>
public sealed class LimitesOptions
{
    public const string Seccion = "Limites";

    [Range(1, 100_000)]
    public int IntentosPorMinuto { get; init; } = 5;
}