using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Fotos;

namespace PosComercio.Api.Controllers;

[Route("api/productos")]
public sealed class FotosController(IFotoService fotoService) : PosControllerBase
{
    // Límite de la petición completa: la foto (10 MB) + un margen para los datos del formulario
    private const long LimitePeticion = ProcesadorImagenes.TamanoMaximoBytes + 1_048_576;

    /// <summary>Sube o reemplaza la foto principal de un producto.</summary>
    [HttpPost("{productoId:int}/foto")]
    [RequestSizeLimit(LimitePeticion)]
    public async Task<ActionResult<ProductoDto>> SubirFotoProducto(
        int productoId, IFormFile archivo, CancellationToken ct)
    {
        await using var contenido = archivo.OpenReadStream();
        return Responder(await fotoService.SubirFotoProductoAsync(productoId, contenido, ct));
    }

    /// <summary>Sube o reemplaza la foto de una variante.</summary>
    [HttpPost("variantes/{varianteId:int}/foto")]
    [RequestSizeLimit(LimitePeticion)]
    public async Task<ActionResult<ProductoDto>> SubirFotoVariante(
        int varianteId, IFormFile archivo, CancellationToken ct)
    {
        await using var contenido = archivo.OpenReadStream();
        return Responder(await fotoService.SubirFotoVarianteAsync(varianteId, contenido, ct));
    }

    /// <summary>Quita la foto principal de un producto.</summary>
    [HttpDelete("{productoId:int}/foto")]
    public async Task<ActionResult<ProductoDto>> EliminarFotoProducto(int productoId, CancellationToken ct) =>
        Responder(await fotoService.EliminarFotoProductoAsync(productoId, ct));
}