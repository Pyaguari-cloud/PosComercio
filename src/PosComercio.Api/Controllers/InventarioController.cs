using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Inventario;
using PosComercio.Api.Services.Inventario;

namespace PosComercio.Api.Controllers;

[Route("api/inventario")]
public sealed class InventarioController(IInventarioService inventario) : PosControllerBase
{
    /// <summary>Pasa mercadería de bodega a percha o viceversa.</summary>
    [HttpPost("traspasos")]
    public async Task<ActionResult<MovimientoRealizadoDto>> Traspasar(TraspasoRequest request, CancellationToken ct) =>
        Responder(await inventario.TraspasarAsync(request, ct));

    /// <summary>Registra la llegada de mercadería. Solo el dueño.</summary>
    [HttpPost("compras")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<CompraRealizadaDto>> RegistrarCompra(CompraRequest request, CancellationToken ct) =>
        Responder(await inventario.RegistrarCompraAsync(request, ct));

    /// <summary>Pérdida, daño o hallazgo. Requiere autorización del dueño.</summary>
    [HttpPost("ajustes")]
    public async Task<ActionResult<MovimientoRealizadoDto>> RegistrarAjuste(AjusteRequest request, CancellationToken ct) =>
        Responder(await inventario.RegistrarAjusteAsync(request, ct));

    /// <summary>Conteo físico: ajusta el stock a lo contado. Solo el dueño.</summary>
    [HttpPost("conteos")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<ConteoResultadoDto>> RegistrarConteo(ConteoRequest request, CancellationToken ct) =>
        Responder(await inventario.RegistrarConteoAsync(request, ct));

    /// <summary>Variantes con 0 en percha y stock en bodega.</summary>
    [HttpGet("reponer-percha")]
    public async Task<ActionResult<IReadOnlyList<ReponerPerchaItemDto>>> ReponerPercha(CancellationToken ct) =>
        Ok(await inventario.ListarReponerPerchaAsync(ct));

    /// <summary>Variantes en o por debajo de su stock mínimo.</summary>
    [HttpGet("stock-bajo")]
    public async Task<ActionResult<IReadOnlyList<StockBajoItemDto>>> StockBajo(CancellationToken ct) =>
        Ok(await inventario.ListarStockBajoAsync(ct));

    /// <summary>Historial de movimientos de una variante.</summary>
    [HttpGet("variantes/{varianteId:int}/kardex")]
    public async Task<ActionResult<PaginaDto<KardexItemDto>>> Kardex(
        int varianteId, [FromQuery] PaginacionQuery query, CancellationToken ct) =>
        Responder(await inventario.ObtenerKardexAsync(varianteId, query, ct));
}