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

    /// <summary>Todos los usuarios del negocio, incluidos los inactivos. Solo el dueño.</summary>
    [HttpGet("gestion")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<IReadOnlyList<UsuarioGestionDto>>> ListarParaGestion(CancellationToken ct) =>
        Ok(await usuarioService.ListarParaGestionAsync(ct));

    /// <summary>Crea un usuario en el negocio. Solo el dueño.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<UsuarioGestionDto>> Crear(CrearUsuarioRequest request, CancellationToken ct) =>
        Responder(await usuarioService.CrearAsync(request, ct));

    /// <summary>Edita nombre, rol o estado (activo/inactivo). Solo el dueño.</summary>
    [HttpPut("{usuarioId:int}")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<UsuarioGestionDto>> Actualizar(
        int usuarioId, ActualizarUsuarioRequest request, CancellationToken ct) =>
        Responder(await usuarioService.ActualizarAsync(usuarioId, request, ct));

    /// <summary>Asigna una contraseña nueva a un usuario que la olvidó. Solo el dueño.</summary>
    [HttpPost("{usuarioId:int}/restablecer-password")]
    [Authorize(Policy = Politicas.SoloDueno)]
    public async Task<ActionResult<UsuarioGestionDto>> RestablecerPassword(
        int usuarioId, RestablecerPasswordRequest request, CancellationToken ct) =>
        Responder(await usuarioService.RestablecerPasswordAsync(usuarioId, request.PasswordNueva, ct));
}