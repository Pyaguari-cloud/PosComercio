using System.Text.Json;
using PosComercio.Api.Services.Catalogo;

namespace PosComercio.Api.Tests.Unitarias;

public class AtributosTests
{
    [Theory]
    [InlineData("40", "Negro", "Talla 40 | Negro")]
    [InlineData("40", null, "Talla 40")]
    [InlineData(null, "Negro", "Negro")]
    [InlineData(" 40 ", "   ", "Talla 40")]
    public void Resumen_CombinaTallaYColor(string? talla, string? color, string esperado)
    {
        Assert.Equal(esperado, Atributos.Resumen(talla, color));
    }

    [Theory]
    [InlineData("  nk-urb-40 ", "NK-URB-40")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizarSku_LimpiaYPasaAMayusculas(string? sku, string? esperado)
    {
        Assert.Equal(esperado, Atributos.NormalizarSku(sku));
    }

    [Fact]
    public void Clave_IgnoraMayusculasYEspacios()
    {
        Assert.Equal(Atributos.Clave("40", "Negro"), Atributos.Clave(" 40", "NEGRO "));
    }

    [Fact]
    public void Json_GeneraTallaYColor()
    {
        var json = Atributos.Json("40", "Café");

        using var documento = JsonDocument.Parse(json);
        Assert.Equal("40", documento.RootElement.GetProperty("talla").GetString());
        Assert.Equal("Café", documento.RootElement.GetProperty("color").GetString());
    }
}