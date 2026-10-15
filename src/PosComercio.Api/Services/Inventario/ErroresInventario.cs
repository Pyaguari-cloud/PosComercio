using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Inventario;

public static class ErroresInventario
{
    public static Error StockInsuficiente(string producto, string ubicacion) => new(
        "inventario.stock_insuficiente",
        $"No hay suficiente stock de '{producto}' en {ubicacion.ToLowerInvariant()}.",
        StatusCodes.Status409Conflict);

    public static readonly Error ItemsRepetidos = new(
        "inventario.items_repetidos",
        "Hay productos repetidos con el mismo destino. Junta las cantidades en una sola línea.",
        StatusCodes.Status400BadRequest);
}