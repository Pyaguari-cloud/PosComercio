using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Usuarios;

public static class ReglasPassword
{
    public const int LongitudMinima = 8;

    /// <summary>Mínimo 8 caracteres, con al menos una letra y un número.</summary>
    public static Error? Validar(string password)
    {
        var esValida = password.Length >= LongitudMinima
                    && password.Any(char.IsLetter)
                    && password.Any(char.IsDigit);

        return esValida ? null : ErroresUsuarios.PasswordDebil;
    }
}