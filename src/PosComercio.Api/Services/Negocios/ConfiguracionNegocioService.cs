using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;

namespace PosComercio.Api.Services.Negocios;

/// <summary>
/// Configuración del negocio. Los valores por defecto se usan si el JSON
/// no trae una propiedad o está vacío.
/// </summary>
public sealed record ConfiguracionNegocio(
    bool CierreCiego = false,
    decimal MontoInicialSugerido = 0,
    bool PermiteRegateo = true);

public interface IConfiguracionNegocioService
{
    Task<ConfiguracionNegocio> ObtenerAsync(CancellationToken ct);
}

public sealed class ConfiguracionNegocioService(
    PosComercioDbContext db,
    ITenantContext tenant,
    ILogger<ConfiguracionNegocioService> logger) : IConfiguracionNegocioService
{
    // camelCase e insensible a mayúsculas, igual que el JSON guardado
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    // Se lee una sola vez por petición, aunque se pida varias veces
    private ConfiguracionNegocio? _cache;

    public async Task<ConfiguracionNegocio> ObtenerAsync(CancellationToken ct)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var json = await db.Negocios
            .AsNoTracking()
            .Where(n => n.NegocioId == tenant.NegocioId)
            .Select(n => n.ConfiguracionJson)
            .FirstOrDefaultAsync(ct);

        _cache = Deserializar(json);
        return _cache;
    }

    private ConfiguracionNegocio Deserializar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ConfiguracionNegocio();
        }

        try
        {
            return JsonSerializer.Deserialize<ConfiguracionNegocio>(json, OpcionesJson) ?? new ConfiguracionNegocio();
        }
        catch (JsonException ex)
        {
            // Un JSON mal escrito no debe tumbar la caja: se usan los valores por defecto
            logger.LogError(ex, "ConfiguracionJson inválida en el negocio {NegocioId}", tenant.NegocioId);
            return new ConfiguracionNegocio();
        }
    }
}