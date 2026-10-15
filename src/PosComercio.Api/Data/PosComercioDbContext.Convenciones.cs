using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PosComercio.Api.Data;

/// <summary>
/// Parte "manual" del DbContext. El resto lo generó el scaffold.
/// Como es una clase partial en otro archivo, un nuevo scaffold no la sobrescribe.
/// </summary>
public partial class PosComercioDbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas las fechas de la base están en UTC: que .NET lo sepa al leerlas
        configurationBuilder.Properties<DateTime>().HaveConversion<ConvertidorFechaUtc>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<ConvertidorFechaUtc>();
    }
}

/// <summary>Marca como UTC las fechas leídas de SQL Server (datetime2 no guarda la zona horaria).</summary>
public sealed class ConvertidorFechaUtc() : ValueConverter<DateTime, DateTime>(
    alGuardar => alGuardar,
    alLeer => DateTime.SpecifyKind(alLeer, DateTimeKind.Utc));