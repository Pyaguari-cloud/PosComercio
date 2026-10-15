using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Usuarios;

public static class ErroresUsuarios
{
    public static readonly Error NoEncontrado = new(
        "usuarios.no_encontrado",
        "El usuario no existe o está inactivo.",
        StatusCodes.Status404NotFound);

    public static readonly Error PinDebil = new(
        "usuarios.pin_debil",
        "El PIN es muy fácil de adivinar. Evita números repetidos (1111) o seguidos (1234).",
        StatusCodes.Status400BadRequest);

    public static readonly Error PasswordDebil = new(
    "usuarios.password_debil",
    "La contraseña debe tener al menos 8 caracteres, con letras y números.",
    StatusCodes.Status400BadRequest);

    public static readonly Error LoginEnUso = new(
        "usuarios.login_en_uso",
        "Ese usuario (correo o celular) ya está registrado.",
        StatusCodes.Status409Conflict);

    public static readonly Error NoPuedesQuitarteAcceso = new(
        "usuarios.no_puedes_quitarte_acceso",
        "No puedes desactivarte ni quitarte el rol de dueño a ti mismo.",
        StatusCodes.Status409Conflict);

    public static readonly Error UltimoDueno = new(
        "usuarios.ultimo_dueno",
        "El negocio debe tener al menos un dueño activo.",
        StatusCodes.Status409Conflict);
}