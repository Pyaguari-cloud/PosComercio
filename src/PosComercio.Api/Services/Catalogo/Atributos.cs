using System.Text.Json;

namespace PosComercio.Api.Services.Catalogo;

/// <summary>
/// Normaliza y construye los atributos de una variante (talla, color, SKU).
/// Funciones puras: misma entrada, misma salida.
/// </summary>
public static class Atributos
{
    /// <summary>Quita espacios; si queda vacío, devuelve null.</summary>
    public static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    /// <summary>"  nk-urb-40 " → "NK-URB-40"</summary>
    public static string? NormalizarSku(string? sku) =>
        Limpiar(sku)?.ToUpperInvariant();

    /// <summary>Clave para comparar variantes sin importar mayúsculas: "40|NEGRO".</summary>
    public static string Clave(string? talla, string? color) =>
        $"{Limpiar(talla)?.ToUpperInvariant()}|{Limpiar(color)?.ToUpperInvariant()}";

    /// <summary>"Talla 40 | Negro", "Talla 40" o "Negro".</summary>
    public static string Resumen(string? talla, string? color)
    {
        var partes = new List<string>();
        if (Limpiar(talla) is { } t) partes.Add($"Talla {t}");
        if (Limpiar(color) is { } c) partes.Add(c);
        return string.Join(" | ", partes);
    }

    /// <summary>{"talla":"40","color":"Negro"}, solo con los atributos presentes.</summary>
    public static string Json(string? talla, string? color)
    {
        var atributos = new Dictionary<string, string>();
        if (Limpiar(talla) is { } t) atributos["talla"] = t;
        if (Limpiar(color) is { } c) atributos["color"] = c;
        return JsonSerializer.Serialize(atributos);
    }
}