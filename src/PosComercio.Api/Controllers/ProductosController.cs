using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Productos;

namespace PosComercio.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(IProductoService productoService) : ControllerBase
{
    /// <summary>Busca productos por texto, talla o categoría, con paginación.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginaDto<ProductoDto>>> Buscar(
        [FromQuery] BuscarProductosQuery query,
        CancellationToken ct)
    {
        var resultado = await productoService.BuscarAsync(query, ct);
        return Ok(resultado);
    }

    /// <summary>Detalle de un producto con todas sus tallas/colores.</summary>
    [HttpGet("{productoId:int}")]
    public async Task<ActionResult<ProductoDto>> ObtenerPorId(int productoId, CancellationToken ct)
    {
        var producto = await productoService.ObtenerPorIdAsync(productoId, ct);
        return producto is null ? NotFound() : Ok(producto);
    }
}