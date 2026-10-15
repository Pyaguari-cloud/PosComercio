using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Comunes;

public sealed record PaginacionQuery
{
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;
}