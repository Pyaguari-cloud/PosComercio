using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Dtos.Ventas;
using PosComercio.Api.Services.Ventas;

namespace PosComercio.Api.Controllers;

[Route("api/ventas")]
public sealed class VentasController(IVentaService ventaService) : PosControllerBase
{
    /// <summary>Registra una venta con sus productos y pagos.</summary>
    [HttpPost]
    public async Task<ActionResult<VentaDto>> Registrar(RegistrarVentaRequest request, CancellationToken ct)
    {
        var resultado = await ventaService.RegistrarAsync(request, ct);

        if (!resultado.Exito)
        {
            return Problema(resultado.Error);
        }

        // 201 Created + cabecera Location con la URL de la venta nueva
        return CreatedAtAction(nameof(Obtener), new { ventaId = resultado.Valor.VentaId }, resultado.Valor);
    }

    /// <summary>Detalle de una venta.</summary>
    [HttpGet("{ventaId:long}")]
    public async Task<ActionResult<VentaDto>> Obtener(long ventaId, CancellationToken ct) =>
        Responder(await ventaService.ObtenerAsync(ventaId, ct));
}