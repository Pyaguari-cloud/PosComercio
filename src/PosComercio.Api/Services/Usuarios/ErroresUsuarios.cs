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
}