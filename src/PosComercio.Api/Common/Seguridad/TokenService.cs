using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Common.Seguridad;

public sealed record TokenGenerado(string AccessToken, DateTime ExpiraEnUtc);

public interface ITokenService
{
    TokenGenerado Generar(Usuario usuario);
}

public sealed class TokenService(IOptions<JwtOptions> opciones, TimeProvider tiempo) : ITokenService
{
    private readonly JwtOptions _jwt = opciones.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenGenerado Generar(Usuario usuario)
    {
        var ahora = tiempo.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(_jwt.ExpiracionMinutos);

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [ClaimsPos.UsuarioId] = usuario.UsuarioId.ToString(),
                [ClaimsPos.NegocioId] = usuario.NegocioId.ToString(),
                [ClaimsPos.Nombre] = usuario.Nombre,
                [ClaimsPos.Rol] = usuario.Rol,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return new TokenGenerado(_handler.CreateToken(descriptor), expira);
    }
}