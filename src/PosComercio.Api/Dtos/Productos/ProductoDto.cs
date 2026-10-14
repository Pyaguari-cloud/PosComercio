namespace PosComercio.Api.Dtos.Productos;

public sealed record ProductoDto(
    int ProductoId,
    string Nombre,
    string? Marca,
    string? Categoria,
    string? CategoriaColor,
    string? ImagenUrl,
    bool TieneVariantes,
    decimal? PrecioDesde,
    int StockPercha,
    int StockBodega,
    IReadOnlyList<VarianteDto> Variantes);