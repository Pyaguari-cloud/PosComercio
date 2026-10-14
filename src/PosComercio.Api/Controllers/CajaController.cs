using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Dtos.Caja;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Services.Caja;

namespace PosComercio.Api.Controllers;

[Route("api/caja")]
public sealed class CajaController(ICajaService cajaService) : PosControllerBase
{
    /// <summary>Estado de la caja: si está abierta, con el resumen en vivo.</summary>
    [HttpGet("actual")]
    public async Task<ActionResult<EstadoCajaDto>> ObtenerActual(CancellationToken ct) =>
        Ok(await cajaService.ObtenerEstadoAsync(ct));

    /// <summary>Abre la caja con el monto inicial (sencillo).</summary>
    [HttpPost("abrir")]
    public async Task<ActionResult<EstadoCajaDto>> Abrir(AbrirCajaRequest request, CancellationToken ct) =>
        Responder(await cajaService.AbrirAsync(request, ct));

    /// <summary>Registra un gasto, retiro o ingreso extra en la caja abierta.</summary>
    [HttpPost("movimientos")]
    public async Task<ActionResult<MovimientoCajaDto>> RegistrarMovimiento(
        RegistrarMovimientoCajaRequest request, CancellationToken ct) =>
        Responder(await cajaService.RegistrarMovimientoAsync(request, ct));

    /// <summary>Cierra la caja con el efectivo contado.</summary>
    [HttpPost("cerrar")]
    public async Task<ActionResult<CierreCajaDto>> Cerrar(CerrarCajaRequest request, CancellationToken ct) =>
        Responder(await cajaService.CerrarAsync(request, ct));

    /// <summary>Historial de cierres. Solo el dueño.</summary>
    [HttpGet("cierres")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<PaginaDto<CierreCajaDto>>> ListarCierres(
        [FromQuery] PaginacionQuery query, CancellationToken ct) =>
        Ok(await cajaService.ListarCierresAsync(query, ct));
}