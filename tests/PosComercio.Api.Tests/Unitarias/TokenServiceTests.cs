using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Tests.Unitarias;

public class TokenServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 15, 14, 0, 0, TimeSpan.Zero);

    private static TokenService CrearServicio() => new(
        Options.Create(new JwtOptions
        {
            Issuer = "PosComercio.Api",
            Audience = "PosComercio.App",
            Key = new string('k', 48),          // clave de prueba: solo vive en esta prueba
            ExpiracionMinutos = 720
        }),
        new FakeTimeProvider(Ahora));

    private static Usuario UsuarioDePrueba() => new()
    {
        UsuarioId = 7,
        NegocioId = 3,
        Nombre = "Vendedor de Prueba",
        Rol = Roles.Vendedor,
        UsuarioLogin = "prueba@demo.com",
        PasswordHash = "no-importa"
    };

    [Fact]
    public void Generar_IncluyeLosDatosDelUsuarioEnElToken()
    {
        var token = CrearServicio().Generar(UsuarioDePrueba());

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);

        Assert.Equal("7", jwt.GetClaim(ClaimsPos.UsuarioId).Value);
        Assert.Equal("3", jwt.GetClaim(ClaimsPos.NegocioId).Value);
        Assert.Equal(Roles.Vendedor, jwt.GetClaim(ClaimsPos.Rol).Value);
        Assert.Equal("PosComercio.Api", jwt.Issuer);
    }

    [Fact]
    public void Generar_ExpiraDoceHorasDespues()
    {
        var token = CrearServicio().Generar(UsuarioDePrueba());

        var esperado = Ahora.UtcDateTime.AddMinutes(720);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);

        Assert.Equal(esperado, token.ExpiraEnUtc);
        Assert.Equal(esperado, jwt.ValidTo);
    }
}