using PosComercio.Api.Services.Usuarios;

namespace PosComercio.Api.Tests.Unitarias;

public class ReglasPinTests
{
    [Theory]
    [InlineData("0000")]
    [InlineData("1111")]
    [InlineData("1234")]
    [InlineData("4321")]
    [InlineData("6789")]
    [InlineData("9876")]
    [InlineData("0123")]
    public void EsDebil_PinRepetidoOSecuencia_DevuelveTrue(string pin)
    {
        Assert.True(ReglasPin.EsDebil(pin));
    }

    [Theory]
    [InlineData("1357")]
    [InlineData("2580")]
    [InlineData("4826")]
    [InlineData("9753")]
    [InlineData("1029")]
    public void EsDebil_PinVariado_DevuelveFalse(string pin)
    {
        Assert.False(ReglasPin.EsDebil(pin));
    }
}