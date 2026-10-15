using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Almacenamiento;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Catalogo;
using PosComercio.Api.Services.Productos;

namespace PosComercio.Api.Services.Fotos;

public interface IFotoService
{
    Task<Resultado<ProductoDto>> SubirFotoProductoAsync(int productoId, Stream contenido, CancellationToken ct);
    Task<Resultado<ProductoDto>> SubirFotoVarianteAsync(int varianteId, Stream contenido, CancellationToken ct);
    Task<Resultado<ProductoDto>> EliminarFotoProductoAsync(int productoId, CancellationToken ct);
}

public sealed class FotoService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IProcesadorImagenes procesador,
    IAlmacenamientoArchivos almacenamiento,
    IProductoService productos,
    ILogger<FotoService> logger) : IFotoService
{
    public async Task<Resultado<ProductoDto>> SubirFotoProductoAsync(
        int productoId, Stream contenido, CancellationToken ct)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p =>
            p.NegocioId == tenant.NegocioId && p.ProductoId == productoId && p.Activo, ct);

        if (producto is null)
        {
            return ErroresCatalogo.ProductoNoEncontrado;
        }

        var urlNueva = await GuardarImagenAsync(contenido, "productos", ct);
        if (!urlNueva.Exito)
        {
            return urlNueva.Error;
        }

        var urlAnterior = producto.ImagenUrl;
        producto.ImagenUrl = urlNueva.Valor;

        if (await ConfirmarCambioAsync(urlNueva.Valor, urlAnterior, ct) is { } error)
        {
            return error;
        }

        return await ObtenerProductoAsync(productoId, ct);
    }

    public async Task<Resultado<ProductoDto>> SubirFotoVarianteAsync(
        int varianteId, Stream contenido, CancellationToken ct)
    {
        var variante = await db.ProductoVariantes.FirstOrDefaultAsync(v =>
            v.NegocioId == tenant.NegocioId && v.VarianteId == varianteId, ct);

        if (variante is null)
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        var urlNueva = await GuardarImagenAsync(contenido, "variantes", ct);
        if (!urlNueva.Exito)
        {
            return urlNueva.Error;
        }

        var urlAnterior = variante.ImagenVarianteUrl;
        variante.ImagenVarianteUrl = urlNueva.Valor;

        if (await ConfirmarCambioAsync(urlNueva.Valor, urlAnterior, ct) is { } error)
        {
            return error;
        }

        return await ObtenerProductoAsync(variante.ProductoId, ct);
    }

    public async Task<Resultado<ProductoDto>> EliminarFotoProductoAsync(int productoId, CancellationToken ct)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p =>
            p.NegocioId == tenant.NegocioId && p.ProductoId == productoId && p.Activo, ct);

        if (producto is null)
        {
            return ErroresCatalogo.ProductoNoEncontrado;
        }

        var urlAnterior = producto.ImagenUrl;
        producto.ImagenUrl = null;
        await db.SaveChangesAsync(ct);

        await EliminarSinFallarAsync(urlAnterior);

        return await ObtenerProductoAsync(productoId, ct);
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    /// <summary>Procesa la imagen y la guarda con un nombre aleatorio. Devuelve su URL.</summary>
    private async Task<Resultado<string>> GuardarImagenAsync(Stream contenido, string carpeta, CancellationToken ct)
    {
        var procesada = await procesador.ProcesarAsync(contenido, ct);
        if (!procesada.Exito)
        {
            return procesada.Error;
        }

        await using var datos = procesada.Valor.Contenido;

        // Ej: "1/productos/3f2a9c1e4b5d4e8f9a0b1c2d3e4f5a6b.webp"
        // El nombre aleatorio hace la URL imposible de adivinar y evita choques entre fotos
        var rutaRelativa = $"{tenant.NegocioId}/{carpeta}/{Guid.NewGuid():N}{procesada.Valor.Extension}";

        return await almacenamiento.GuardarAsync(datos, rutaRelativa, procesada.Valor.ContentType, ct);
    }

    /// <summary>
    /// Guarda el cambio en la base. Si falla, borra la foto nueva (para no dejar basura).
    /// Si sale bien, borra la foto anterior.
    /// </summary>
    private async Task<Error?> ConfirmarCambioAsync(string urlNueva, string? urlAnterior, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await EliminarSinFallarAsync(urlNueva);
            return ErroresCatalogo.ModificadaPorOtro;
        }
        catch
        {
            await EliminarSinFallarAsync(urlNueva);
            throw;   // relanza la excepción original, con su información intacta
        }

        await EliminarSinFallarAsync(urlAnterior);
        return null;
    }

    /// <summary>
    /// Borra un archivo sin interrumpir la operación si falla.
    /// Un archivo huérfano es una molestia menor; perder el cambio en la base sería peor.
    /// </summary>
    private async Task EliminarSinFallarAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            // CancellationToken.None: la limpieza no debe cancelarse aunque el usuario cierre la app
            await almacenamiento.EliminarAsync(url, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo eliminar el archivo {Url}. Quedará huérfano.", url);
        }
    }

    private async Task<Resultado<ProductoDto>> ObtenerProductoAsync(int productoId, CancellationToken ct)
    {
        var dto = await productos.ObtenerPorIdAsync(productoId, ct);

        if (dto is null)
        {
            return ErroresCatalogo.ProductoNoEncontrado;
        }

        return dto;
    }
}