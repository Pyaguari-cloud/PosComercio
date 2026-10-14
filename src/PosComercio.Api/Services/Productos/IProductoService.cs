using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Productos;

namespace PosComercio.Api.Services.Productos;

public interface IProductoService
{
    Task<PaginaDto<ProductoDto>> BuscarAsync(BuscarProductosQuery query, CancellationToken ct);
    Task<ProductoDto?> ObtenerPorIdAsync(int productoId, CancellationToken ct);
}