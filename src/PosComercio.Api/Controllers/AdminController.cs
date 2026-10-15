using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Dtos.Admin;
using PosComercio.Api.Services.Admin;

namespace PosComercio.Api.Controllers;

/// <summary>Panel del administrador del SaaS. Todo el controller exige rol AdminSaaS.</summary>
[Route("api/admin/negocios")]
[Authorize(Policy = Politicas.SoloAdminSaaS)]
public sealed class AdminController(IAdminService adminService) : PosControllerBase
{
    /// <summary>Lista los negocios clientes.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NegocioAdminDto>>> Listar(CancellationToken ct) =>
        Ok(await adminService.ListarNegociosAsync(ct));

    /// <summary>Da de alta un negocio nuevo con su dueño.</summary>
    [HttpPost]
    public async Task<ActionResult<NegocioAdminDto>> Crear(CrearNegocioRequest request, CancellationToken ct) =>
        Responder(await adminService.CrearNegocioAsync(request, ct));

    /// <summary>Cambia el plan de un negocio (por ejemplo, SUSPENDIDO por falta de pago).</summary>
    [HttpPut("{negocioId:int}/plan")]
    public async Task<ActionResult<NegocioAdminDto>> CambiarPlan(
        int negocioId, CambiarPlanRequest request, CancellationToken ct) =>
        Responder(await adminService.CambiarPlanAsync(negocioId, request, ct));
}