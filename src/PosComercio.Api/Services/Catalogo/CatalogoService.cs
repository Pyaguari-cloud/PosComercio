using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Catalogo;

namespace PosComercio.Api.Services.Catalogo;

public interface ICatalogoService
{
    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct);
    Task<Resultado<CategoriaDto>> CrearCategoriaAsync(CrearCategoriaRequest request, CancellationToken ct);
    Task<IReadOnlyList<UbicacionDto>> ListarUbicacionesAsync(CancellationToken ct);
    Task<Resultado<UbicacionDto>> CrearUbicacionAsync(CrearUbicacionRequest request, CancellationToken ct);
}

public sealed class CatalogoService(PosComercioDbContext db, ITenantContext tenant) : ICatalogoService
{
    public async Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct) =>
        await db.Set<Categoria>()
            .AsNoTracking()
            .Where(c => c.NegocioId == tenant.NegocioId && c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.CategoriaId, c.Nombre, c.ColorHex))
            .ToListAsync(ct);

    public async Task<Resultado<CategoriaDto>> CrearCategoriaAsync(CrearCategoriaRequest request, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim();

        var existe = await db.Set<Categoria>().AnyAsync(c =>
            c.NegocioId == tenant.NegocioId && c.Activo && c.Nombre == nombre, ct);

        if (existe)
        {
            return ErroresCatalogo.CategoriaDuplicada;
        }

        var categoria = new Categoria
        {
            NegocioId = tenant.NegocioId,
            Nombre = nombre,
            ColorHex = request.ColorHex.ToUpperInvariant(),
            Activo = true
        };

        db.Add(categoria);
        await db.SaveChangesAsync(ct);

        return new CategoriaDto(categoria.CategoriaId, categoria.Nombre, categoria.ColorHex);
    }

    public async Task<IReadOnlyList<UbicacionDto>> ListarUbicacionesAsync(CancellationToken ct) =>
        await db.Set<UbicacionesBodega>()
            .AsNoTracking()
            .Where(u => u.NegocioId == tenant.NegocioId && u.Activo)
            .OrderBy(u => u.Nombre)
            .Select(u => new UbicacionDto(u.UbicacionId, u.Nombre, u.Descripcion))
            .ToListAsync(ct);

    public async Task<Resultado<UbicacionDto>> CrearUbicacionAsync(CrearUbicacionRequest request, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim();

        var existe = await db.Set<UbicacionesBodega>().AnyAsync(u =>
            u.NegocioId == tenant.NegocioId && u.Activo && u.Nombre == nombre, ct);

        if (existe)
        {
            return ErroresCatalogo.UbicacionDuplicada;
        }

        var ubicacion = new UbicacionesBodega
        {
            NegocioId = tenant.NegocioId,
            Nombre = nombre,
            Descripcion = Atributos.Limpiar(request.Descripcion),
            Activo = true
        };

        db.Add(ubicacion);
        await db.SaveChangesAsync(ct);

        return new UbicacionDto(ubicacion.UbicacionId, ubicacion.Nombre, ubicacion.Descripcion);
    }
}