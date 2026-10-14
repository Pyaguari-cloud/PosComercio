using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Productos;

/// <summary>
/// Filtros que llegan por la URL: /api/productos?buscar=nike&talla=40&pagina=1
/// </summary>
public sealed class BuscarProductosQuery
{
    [StringLength(100)]
    public string? Buscar { get; init; }

    [StringLength(20)]
    public string? Talla { get; init; }

    public int? CategoriaId { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;
}