namespace PosComercio.Api.Common.Tenancy;

/// <summary>
/// Da acceso al negocio (tenant) de la petición actual.
/// Los servicios dependen de esta interfaz, no de cómo se obtiene el dato.
/// </summary>
public interface ITenantContext
{
    int NegocioId { get; }
}