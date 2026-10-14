using System.Diagnostics.CodeAnalysis;

namespace PosComercio.Api.Common.Resultados;

/// <summary>
/// El resultado de una operación: o tiene un Valor (éxito) o tiene un Error (fallo).
/// </summary>
public sealed class Resultado<T>
{
    private Resultado(T? valor, Error? error)
    {
        Valor = valor;
        Error = error;
    }

    public T? Valor { get; }
    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Valor))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Exito => Error is null;

    public static Resultado<T> Ok(T valor) => new(valor, null);
    public static Resultado<T> Fallo(Error error) => new(default, error);

    // Permiten escribir "return dto;" o "return ErroresAuth.X;" directamente
    public static implicit operator Resultado<T>(T valor) => Ok(valor);
    public static implicit operator Resultado<T>(Error error) => Fallo(error);
}