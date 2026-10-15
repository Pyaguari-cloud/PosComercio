using Microsoft.AspNetCore.Identity;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Common.Seguridad;

public static class PasswordHasherExtensions
{
    /// <summary>
    /// Verifica un hash sin lanzar excepción si el hash guardado tiene un formato inválido.
    /// </summary>
    public static PasswordVerificationResult VerificarSeguro(
        this IPasswordHasher<Usuario> hasher, Usuario usuario, string hashGuardado, string valor)
    {
        try
        {
            return hasher.VerifyHashedPassword(usuario, hashGuardado, valor);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}