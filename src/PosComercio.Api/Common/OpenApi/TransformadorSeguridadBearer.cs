using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PosComercio.Api.Common.OpenApi;

/// <summary>Declara en la documentación que la API usa tokens JWT (Bearer).</summary>
internal sealed class TransformadorSeguridadBearer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Pega aquí el accessToken que devuelve /api/auth/login."
        };

        return Task.CompletedTask;
    }
}