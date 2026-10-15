using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Common.Seguridad;

namespace PosComercio.Api.Services.Productos;

public sealed class ProductoService(PosComercioDbContext db, ITenantContext tenant, IUsuarioActual usuarioActual) : IProductoService
{
    public async Task<PaginaDto<ProductoDto>> BuscarAsync(BuscarProductosQuery query, CancellationToken ct)
    {
        IQueryable<Producto> consulta = ProductosDelNegocio();

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            var texto = query.Buscar.Trim();
            consulta = consulta.Where(p =>
                p.Nombre.Contains(texto) ||
                (p.Marca != null && p.Marca.Contains(texto)) ||
                p.ProductoVariantes.Any(v => v.CodigoSku == texto));
        }

        if (!string.IsNullOrWhiteSpace(query.Talla))
        {
            var talla = query.Talla.Trim();
            consulta = consulta.Where(p => p.ProductoVariantes.Any(v =>
                v.Activo && v.Talla == talla && (v.StockPercha + v.StockBodega) > 0));
        }

        if (query.CategoriaId is not null)
        {
            consulta = consulta.Where(p => p.CategoriaId == query.CategoriaId);
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(p => p.Nombre)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .Select(AProductoDto(usuarioActual.EsDueno))
            .ToListAsync(ct);

        return new PaginaDto<ProductoDto>(items, query.Pagina, query.TamanoPagina, total);
    }

    public Task<ProductoDto?> ObtenerPorIdAsync(int productoId, CancellationToken ct) =>
        ProductosDelNegocio()
            .Where(p => p.ProductoId == productoId)
            .Select(AProductoDto(usuarioActual.EsDueno))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Punto de partida de TODA consulta de productos: solo los del negocio actual y activos.
    /// Tenerlo en un solo lugar evita olvidar el filtro por NegocioId.
    /// </summary>
    private IQueryable<Producto> ProductosDelNegocio() =>
        db.Productos
          .AsNoTracking()
          .Where(p => p.NegocioId == tenant.NegocioId && p.Activo);

    /// <summary>
    /// Cómo se convierte una entidad Producto en ProductoDto.
    /// Es una Expression (no una función normal) para que EF Core la traduzca a SQL
    /// y traiga solo las columnas necesarias.
    /// </summary>
    private static Expression<Func<Producto, ProductoDto>> AProductoDto(bool verCostos) => p =>
        new ProductoDto(
            p.ProductoId,
            p.Nombre,
            p.Marca,
            p.Categoria != null ? p.Categoria.Nombre : null,
            p.Categoria != null ? p.Categoria.ColorHex : null,
            p.ImagenUrl,
            p.TieneVariantes,
            p.ProductoVariantes.Where(v => v.Activo).Min(v => (decimal?)v.PrecioVentaLista),
            p.ProductoVariantes.Where(v => v.Activo).Sum(v => v.StockPercha),
            p.ProductoVariantes.Where(v => v.Activo).Sum(v => v.StockBodega),
            p.ProductoVariantes
                .Where(v => v.Activo)
                .OrderBy(v => v.Talla)
                .ThenBy(v => v.Color)
                .Select(v => new VarianteDto(
                    v.VarianteId,
                    v.CodigoSku,
                    v.Talla,
                    v.Color,
                    v.AtributosResumen,
                    v.PrecioVentaLista,
                    v.PrecioMinimoRegateo,
                    v.StockPercha,
                    v.StockBodega,
                    v.UbicacionesBodega != null ? v.UbicacionesBodega.Nombre : null,
                    v.ReferenciaBodega,
                    v.ReferenciaPercha,
                    verCostos ? v.PrecioCosto : null,
                    v.ImagenVarianteUrl))
                .ToList());
}