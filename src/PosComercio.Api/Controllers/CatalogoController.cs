using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Dtos.Catalogo;
using PosComercio.Api.Services.Catalogo;

namespace PosComercio.Api.Controllers;

[Route("api")]
public sealed class CatalogoController(ICatalogoService catalogo) : PosControllerBase
{
    [HttpGet("categorias")]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> ListarCategorias(CancellationToken ct) =>
        Ok(await catalogo.ListarCategoriasAsync(ct));

    [HttpPost("categorias")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<CategoriaDto>> CrearCategoria(CrearCategoriaRequest request, CancellationToken ct) =>
        Responder(await catalogo.CrearCategoriaAsync(request, ct));

    [HttpGet("ubicaciones")]
    public async Task<ActionResult<IReadOnlyList<UbicacionDto>>> ListarUbicaciones(CancellationToken ct) =>
        Ok(await catalogo.ListarUbicacionesAsync(ct));

    [HttpPost("ubicaciones")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<UbicacionDto>> CrearUbicacion(CrearUbicacionRequest request, CancellationToken ct) =>
        Responder(await catalogo.CrearUbicacionAsync(request, ct));
}