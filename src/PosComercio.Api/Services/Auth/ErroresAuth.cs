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

    public static readonly Error UsuarioNoDisponible = new(
    "auth.usuario_no_disponible",
    "Ese usuario no existe o está inactivo.",
    StatusCodes.Status404NotFound);

    public static readonly Error PinNoAsignado = new(
        "auth.pin_no_asignado",
        "Este usuario todavía no tiene PIN. Pídele al dueño que le asigne uno.",
        StatusCodes.Status400BadRequest);

    public static readonly Error PinIncorrecto = new(
        "auth.pin_incorrecto",
        "PIN incorrecto.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error PasswordActualIncorrecta = new(
    "auth.password_actual_incorrecta",
    "La contraseña actual no es correcta.",
    StatusCodes.Status400BadRequest);
}