using System.Globalization;
using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Ventas;

public static class ErroresVentas
{
    public static readonly Error NoEncontrada = new(
        "ventas.no_encontrada",
        "La venta no existe.",
        StatusCodes.Status404NotFound);

    public static readonly Error ItemsDuplicados = new(
        "ventas.items_duplicados",
        "Hay productos repetidos con la misma ubicación. Junta las cantidades en una sola línea.",
        StatusCodes.Status400BadRequest);

    public static readonly Error MasDeDosDecimales = new(
        "ventas.mas_de_dos_decimales",
        "Los precios y montos pueden tener máximo 2 decimales.",
        StatusCodes.Status400BadRequest);

    // Errores con datos variables: se crean con un método
    public static Error PagosNoCuadran(decimal total, decimal pagado) => new(
        "ventas.pagos_no_cuadran",
        string.Create(CultureInfo.InvariantCulture,
            $"Los pagos suman ${pagado:0.00}, pero la venta es de ${total:0.00}."),
        StatusCodes.Status422UnprocessableEntity);

    public static Error VarianteNoEncontrada(int varianteId) => new(
        "ventas.variante_no_encontrada",
        $"El producto con id {varianteId} no existe o está inactivo.",
        StatusCodes.Status404NotFound);

    public static Error StockInsuficiente(string producto, string ubicacion) => new(
        "ventas.stock_insuficiente",
        $"No hay suficiente stock de '{producto}' en {ubicacion.ToLowerInvariant()}.",
        StatusCodes.Status409Conflict);
}