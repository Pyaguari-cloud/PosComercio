using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Productos;
using Microsoft.AspNetCore.Authorization;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Services.Catalogo;

namespace PosComercio.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(IProductoService productoService, IProductoEdicionService edicion) : PosControllerBase
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

    /// <summary>Crea un producto con sus variantes y stock inicial. Solo el dueño.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<ProductoDto>> Crear(CrearProductoRequest request, CancellationToken ct)
    {
        var resultado = await edicion.CrearAsync(request, ct);

        if (!resultado.Exito)
        {
            return Problema(resultado.Error);
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { productoId = resultado.Valor.ProductoId }, resultado.Valor);
    }

    /// <summary>Agrega tallas/colores nuevos a un producto existente. Solo el dueño.</summary>
    [HttpPost("{productoId:int}/variantes")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<ProductoDto>> AgregarVariantes(
        int productoId, AgregarVariantesRequest request, CancellationToken ct) =>
        Responder(await edicion.AgregarVariantesAsync(productoId, request, ct));

    /// <summary>Edita precios, ubicación, SKU o estado de una variante. No modifica stock.</summary>
    [HttpPut("variantes/{varianteId:int}")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<ProductoDto>> ActualizarVariante(
        int varianteId, ActualizarVarianteRequest request, CancellationToken ct) =>
        Responder(await edicion.ActualizarVarianteAsync(varianteId, request, ct));
}