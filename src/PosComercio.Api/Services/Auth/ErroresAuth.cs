using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Auth;

public static class ErroresAuth
{
    // Mismo mensaje si el usuario no existe o si la contraseña es incorrecta:
    // así nadie puede averiguar qué usuarios existen probando correos.
    public static readonly Error CredencialesInvalidas = new(
        "auth.credenciales_invalidas",
        "Usuario o contraseña incorrectos.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error NegocioSuspendido = new(
        "auth.negocio_suspendido",
        "La cuenta de este negocio está suspendida. Comunícate con soporte.",
        StatusCodes.Status403Forbidden);
}