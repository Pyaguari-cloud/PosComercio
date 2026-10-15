namespace PosComercio.Api.Common.Tiempo;

/// <summary>
/// Conversión entre el "día local" del negocio y rangos en UTC.
/// Funciones puras: fáciles de probar en la Fase 7.
/// </summary>
public static class RangoFechas
{
    /// <summary>
    /// Devuelve el rango UTC [desde, hasta) que corresponde a un día completo en la zona del negocio.
    /// </summary>
    public static (DateTime DesdeUtc, DateTime HastaUtc) DiaLocalEnUtc(DateOnly fecha, string zonaHoraria)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
        var inicioLocal = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        var desdeUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, zona);
        var hastaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddDays(1), zona);

        return (desdeUtc, hastaUtc);
    }

    /// <summary>La fecha de "hoy" en la zona del negocio.</summary>
    public static DateOnly HoyLocal(TimeProvider tiempo, string zonaHoraria)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
        var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(tiempo.GetUtcNow().UtcDateTime, zona);
        return DateOnly.FromDateTime(ahoraLocal);
    }
}