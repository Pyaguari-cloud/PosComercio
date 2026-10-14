namespace PosComercio.Api.Dtos.Comunes;

/// <summary>
/// Respuesta paginada genérica. La reutilizaremos en ventas, kardex, etc.
/// </summary>
public sealed record PaginaDto<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int Total)
{
    public int TotalPaginas => (int)Math.Ceiling(Total / (double)TamanoPagina);
}