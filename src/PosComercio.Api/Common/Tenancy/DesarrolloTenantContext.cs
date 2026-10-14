namespace PosComercio.Api.Common.Tenancy;

/// <summary>
/// Implementación TEMPORAL para desarrollo: lee el NegocioId de la configuración.
/// Se reemplaza en la Fase 3 por una que lo lea del usuario autenticado.
/// </summary>
public sealed class DesarrolloTenantContext(IConfiguration configuration) : ITenantContext
{
    public int NegocioId { get; } = configuration.GetValue<int?>("Desarrollo:NegocioId")
        ?? throw new InvalidOperationException("Falta 'Desarrollo:NegocioId' en appsettings.Development.json.");
}