using PosComercio.Api.Services.Usuarios;

namespace PosComercio.Api.Tests.Unitarias;

public class ReglasPasswordTests
{
    [Theory]
    [InlineData("Demo123*")]
    [InlineData("venta2026")]
    [InlineData("abcdefg1")]      // justo 8 caracteres: el límite
    public void Validar_PasswordValida_NoDevuelveError(string password)
    {
        Assert.Null(ReglasPassword.Validar(password));
    }

    [Theory]
    [InlineData("abc123")]        // muy corta
    [InlineData("abcdefgh")]      // sin números
    [InlineData("12345678")]      // sin letras
    [InlineData("")]              // vacía
    public void Validar_PasswordDebil_DevuelvePasswordDebil(string password)
    {
        var error = ReglasPassword.Validar(password);

        Assert.Equal(ErroresUsuarios.PasswordDebil, error);
    }
}