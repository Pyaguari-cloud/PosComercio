using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Productos;

public sealed record VarianteNuevaRequest(
    [StringLength(20)] string? Talla,
    [StringLength(40)] string? Color,
    [StringLength(80)] string? CodigoSku,
    [Range(0, 100_000)] decimal PrecioCosto,
    [Range(0.01, 100_000)] decimal PrecioVentaLista,
    [Range(0, 100_000)] decimal PrecioMinimoRegateo,
    [Range(0, 10_000)] int StockPercha,
    [Range(0, 10_000)] int StockBodega,
    [Range(0, 1_000)] int StockMinimoAlerta,
    int? UbicacionBodegaId,
    [StringLength(150)] string? ReferenciaBodega,
    [StringLength(100)] string? ReferenciaPercha);

public sealed record CrearProductoRequest(
    [Required, StringLength(150, MinimumLength = 2)] string Nombre,
    [StringLength(100)] string? Marca,
    [StringLength(300)] string? Descripcion,
    int? CategoriaId,
    bool TieneVariantes,
    [Required, MinLength(1), MaxLength(60)] IReadOnlyList<VarianteNuevaRequest> Variantes);

public sealed record AgregarVariantesRequest(
    [Required, MinLength(1), MaxLength(60)] IReadOnlyList<VarianteNuevaRequest> Variantes);

/// <summary>
/// Edición de una variante. A propósito NO incluye stock:
/// el stock solo cambia con movimientos de inventario (kardex).
/// </summary>
public sealed record ActualizarVarianteRequest(
    [StringLength(80)] string? CodigoSku,
    [Range(0, 100_000)] decimal PrecioCosto,
    [Range(0.01, 100_000)] decimal PrecioVentaLista,
    [Range(0, 100_000)] decimal PrecioMinimoRegateo,
    [Range(0, 1_000)] int StockMinimoAlerta,
    int? UbicacionBodegaId,
    [StringLength(150)] string? ReferenciaBodega,
    [StringLength(100)] string? ReferenciaPercha,
    bool Activo);