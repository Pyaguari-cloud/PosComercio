namespace PosComercio.Api.Dtos.Productos;

/// <summary>
/// Una talla/color de un producto, tal como la ve el vendedor.
/// No incluye PrecioCosto a propósito.
/// </summary>
public sealed record VarianteDto(
    int VarianteId,
    string? CodigoSku,
    string? Talla,
    string? Color,
    string? AtributosResumen,
    decimal PrecioVentaLista,
    decimal PrecioMinimoRegateo,
    int StockPercha,
    int StockBodega,
    string? UbicacionBodega,
    string? ReferenciaBodega,
    string? ReferenciaPercha,
    decimal? PrecioCosto);