using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Dtos.Usuarios;
using PosComercio.Api.Services.Usuarios;

namespace PosComercio.Api.Controllers;

[Route("api/usuarios")]
public sealed class UsuariosController(IUsuarioService usuarioService) : PosControllerBase
{
    /// <summary>Usuarios activos del negocio (para la pantalla de cambio rápido).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioListaDto>>> Listar(CancellationToken ct) =>
        Ok(await usuarioService.ListarActivosAsync(ct));

    /// <summary>Asigna o cambia el PIN de un usuario. Solo el dueño.</summary>
    [HttpPut("{usuarioId:int}/pin")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<UsuarioListaDto>> AsignarPin(
        int usuarioId, AsignarPinRequest request, CancellationToken ct)
    {
        var resultado = await usuarioService.AsignarPinAsync(usuarioId, request.Pin, ct);
        return Responder(resultado);
    }
}