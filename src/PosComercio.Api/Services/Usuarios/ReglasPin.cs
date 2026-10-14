namespace PosComercio.Api.Services.Usuarios;

/// <summary>
/// Reglas de negocio del PIN, separadas para poder probarlas solas en la Fase 7.
/// </summary>
public static class ReglasPin
{
    /// <summary>
    /// Un PIN es débil si todos sus dígitos son iguales (0000, 7777)
    /// o si es una secuencia (1234, 4321, 6789).
    /// </summary>
    public static bool EsDebil(string pin)
    {
        if (pin.Distinct().Count() == 1)
        {
            return true;
        }

        var esAscendente = true;
        var esDescendente = true;

        for (var i = 1; i < pin.Length; i++)
        {
            var diferencia = pin[i] - pin[i - 1];
            if (diferencia != 1) esAscendente = false;
            if (diferencia != -1) esDescendente = false;
        }

        return esAscendente || esDescendente;
    }
}