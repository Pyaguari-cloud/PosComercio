using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Productos;

namespace PosComercio.Api.Services.Catalogo;

public interface IProductoEdicionService
{
    Task<Resultado<ProductoDto>> CrearAsync(CrearProductoRequest request, CancellationToken ct);
    Task<Resultado<ProductoDto>> AgregarVariantesAsync(int productoId, AgregarVariantesRequest request, CancellationToken ct);
    Task<Resultado<ProductoDto>> ActualizarVarianteAsync(int varianteId, ActualizarVarianteRequest request, CancellationToken ct);
}

public sealed class ProductoEdicionService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IProductoService productos,
    TimeProvider tiempo,
    ILogger<ProductoEdicionService> logger) : IProductoEdicionService
{
    private DateTime Ahora => tiempo.GetUtcNow().UtcDateTime;

    // =====================================================================
    // CREAR PRODUCTO
    // =====================================================================

    public async Task<Resultado<ProductoDto>> CrearAsync(CrearProductoRequest request, CancellationToken ct)
    {
        if (ReglasProducto.ValidarVariantesNuevas(request.TieneVariantes, request.Variantes) is { } errorReglas)
        {
            return errorReglas;
        }

        if (await ValidarReferenciasAsync(request.CategoriaId, request.Variantes, ct) is { } errorReferencias)
        {
            return errorReferencias;
        }

        var producto = new Producto
        {
            NegocioId = tenant.NegocioId,
            CategoriaId = request.CategoriaId,
            Nombre = request.Nombre.Trim(),
            Marca = Atributos.Limpiar(request.Marca),
            Descripcion = Atributos.Limpiar(request.Descripcion),
            TieneVariantes = request.TieneVariantes,
            Activo = true,
            FechaCreacion = Ahora
        };

        var variantes = request.Variantes
            .Select(v => CrearVariante(v, request.TieneVariantes))
            .ToList();

        // Al agregarlas a la colección del producto, EF les asigna el ProductoId al guardar
        foreach (var variante in variantes)
        {
            producto.ProductoVariantes.Add(variante);
        }

        db.Productos.Add(producto);

        if (await GuardarConStockInicialAsync(variantes, ct) is { } errorGuardado)
        {
            return errorGuardado;
        }

        logger.LogInformation("Producto {ProductoId} '{Nombre}' creado con {Cantidad} variante(s) por {UsuarioId}",
            producto.ProductoId, producto.Nombre, variantes.Count, usuario.UsuarioId);

        return await ObtenerProductoAsync(producto.ProductoId, ct);
    }

    // =====================================================================
    // AGREGAR TALLAS/COLORES A UN PRODUCTO EXISTENTE
    // =====================================================================

    public async Task<Resultado<ProductoDto>> AgregarVariantesAsync(
        int productoId, AgregarVariantesRequest request, CancellationToken ct)
    {
        var producto = await db.Productos
            .AsNoTracking()
            .Where(p => p.NegocioId == tenant.NegocioId && p.ProductoId == productoId && p.Activo)
            .Select(p => new { p.ProductoId, p.TieneVariantes })
            .FirstOrDefaultAsync(ct);

        if (producto is null)
        {
            return ErroresCatalogo.ProductoNoEncontrado;
        }

        if (!producto.TieneVariantes)
        {
            return ErroresCatalogo.ProductoSinVariantes;
        }

        if (ReglasProducto.ValidarVariantesNuevas(tieneVariantes: true, request.Variantes) is { } errorReglas)
        {
            return errorReglas;
        }

        if (await ValidarReferenciasAsync(categoriaId: null, request.Variantes, ct) is { } errorReferencias)
        {
            return errorReferencias;
        }

        // ¿Alguna de las nuevas ya existe en el producto (incluso desactivada)?
        var existentes = await db.ProductoVariantes
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId && v.ProductoId == productoId)
            .Select(v => new { v.Talla, v.Color })
            .ToListAsync(ct);

        var clavesExistentes = existentes
            .Select(e => Atributos.Clave(e.Talla, e.Color))
            .ToHashSet();

        var yaExiste = request.Variantes.FirstOrDefault(v =>
            clavesExistentes.Contains(Atributos.Clave(v.Talla, v.Color)));

        if (yaExiste is not null)
        {
            return ErroresCatalogo.VarianteYaExiste(yaExiste.Talla, yaExiste.Color);
        }

        var variantes = request.Variantes
            .Select(v => CrearVariante(v, tieneVariantes: true))
            .ToList();

        foreach (var variante in variantes)
        {
            variante.ProductoId = productoId;
            db.ProductoVariantes.Add(variante);
        }

        if (await GuardarConStockInicialAsync(variantes, ct) is { } errorGuardado)
        {
            return errorGuardado;
        }

        logger.LogInformation("{Cantidad} variante(s) agregada(s) al producto {ProductoId} por {UsuarioId}",
            variantes.Count, productoId, usuario.UsuarioId);

        return await ObtenerProductoAsync(productoId, ct);
    }

    // =====================================================================
    // EDITAR UNA VARIANTE (precios, ubicación, SKU, activo; NUNCA stock)
    // =====================================================================

    public async Task<Resultado<ProductoDto>> ActualizarVarianteAsync(
        int varianteId, ActualizarVarianteRequest request, CancellationToken ct)
    {
        if (ReglasProducto.ValidarPrecios(
                request.PrecioCosto, request.PrecioVentaLista, request.PrecioMinimoRegateo) is { } errorPrecios)
        {
            return errorPrecios;
        }

        // Sin AsNoTracking: vamos a modificar esta entidad
        var variante = await db.ProductoVariantes.FirstOrDefaultAsync(v =>
            v.NegocioId == tenant.NegocioId && v.VarianteId == varianteId, ct);

        if (variante is null)
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        if (request.UbicacionBodegaId is int ubicacionId && !await UbicacionExisteAsync(ubicacionId, ct))
        {
            return ErroresCatalogo.UbicacionNoEncontrada;
        }

        var sku = Atributos.NormalizarSku(request.CodigoSku);
        if (sku is not null && sku != variante.CodigoSku && await SkuEnUsoAsync(new[] { sku }, ct))
        {
            return ErroresCatalogo.SkuDuplicado;
        }

        var precioAnterior = variante.PrecioVentaLista;

        variante.CodigoSku = sku;
        variante.PrecioCosto = request.PrecioCosto;
        variante.PrecioVentaLista = request.PrecioVentaLista;
        variante.PrecioMinimoRegateo = request.PrecioMinimoRegateo;
        variante.StockMinimoAlerta = request.StockMinimoAlerta;
        variante.UbicacionBodegaId = request.UbicacionBodegaId;
        variante.ReferenciaBodega = Atributos.Limpiar(request.ReferenciaBodega);
        variante.ReferenciaPercha = Atributos.Limpiar(request.ReferenciaPercha);
        variante.Activo = request.Activo;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Alguien (por ejemplo, una venta) modificó la fila entre nuestra lectura y el guardado
            return ErroresCatalogo.ModificadaPorOtro;
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            return ErroresCatalogo.SkuDuplicado;
        }

        if (precioAnterior != variante.PrecioVentaLista)
        {
            logger.LogInformation("Precio de la variante {VarianteId}: {Anterior} → {Nuevo}, por {UsuarioId}",
                varianteId, precioAnterior, variante.PrecioVentaLista, usuario.UsuarioId);
        }

        return await ObtenerProductoAsync(variante.ProductoId, ct);
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private ProductoVariante CrearVariante(VarianteNuevaRequest v, bool tieneVariantes) => new()
    {
        NegocioId = tenant.NegocioId,
        UbicacionBodegaId = v.UbicacionBodegaId,
        ReferenciaBodega = Atributos.Limpiar(v.ReferenciaBodega),
        ReferenciaPercha = Atributos.Limpiar(v.ReferenciaPercha),
        CodigoSku = Atributos.NormalizarSku(v.CodigoSku),
        EsVarianteUnica = !tieneVariantes,
        AtributosResumen = tieneVariantes ? Atributos.Resumen(v.Talla, v.Color) : null,
        AtributosJson = tieneVariantes ? Atributos.Json(v.Talla, v.Color) : null,
        PrecioCosto = v.PrecioCosto,
        PrecioVentaLista = v.PrecioVentaLista,
        PrecioMinimoRegateo = v.PrecioMinimoRegateo,
        StockPercha = v.StockPercha,
        StockBodega = v.StockBodega,
        StockMinimoAlerta = v.StockMinimoAlerta,
        Activo = true
    };

    /// <summary>
    /// Guarda lo que haya en el contexto (producto y/o variantes) y registra el kardex
    /// del stock inicial, todo en una transacción. Devuelve null si salió bien.
    /// </summary>
    private async Task<Error?> GuardarConStockInicialAsync(
        IReadOnlyList<ProductoVariante> variantes, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. Guarda producto y variantes: aquí SQL Server asigna los IDs
            await db.SaveChangesAsync(ct);

            // 2. Con los VarianteId ya asignados, registra el stock inicial en el kardex
            AgregarMovimientosIniciales(variantes);
            await db.SaveChangesAsync(ct);

            await transaccion.CommitAsync(ct);
            return null;
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            // Otro producto tomó el mismo SKU justo ahora
            db.ChangeTracker.Clear();
            return ErroresCatalogo.SkuDuplicado;
        }
    }

    private void AgregarMovimientosIniciales(IEnumerable<ProductoVariante> variantes)
    {
        var ahora = Ahora;

        foreach (var variante in variantes)
        {
            // Primero la percha y luego la bodega, para que el stock resultante de cada fila sea coherente
            if (variante.StockPercha > 0)
            {
                db.Set<MovimientosInventario>().Add(CrearMovimientoInicial(
                    variante, UbicacionesStock.Percha, variante.StockPercha,
                    perchaResultante: variante.StockPercha, bodegaResultante: 0, ahora));
            }

            if (variante.StockBodega > 0)
            {
                db.Set<MovimientosInventario>().Add(CrearMovimientoInicial(
                    variante, UbicacionesStock.Bodega, variante.StockBodega,
                    perchaResultante: variante.StockPercha, bodegaResultante: variante.StockBodega, ahora));
            }
        }
    }

    private MovimientosInventario CrearMovimientoInicial(
        ProductoVariante variante, string destino, int cantidad,
        int perchaResultante, int bodegaResultante, DateTime fecha) => new()
        {
            NegocioId = tenant.NegocioId,
            VarianteId = variante.VarianteId,
            UsuarioId = usuario.UsuarioId,
            TipoMovimiento = TiposMovimientoInventario.Inicial,
            UbicacionOrigen = null,
            UbicacionDestino = destino,
            Cantidad = cantidad,
            StockPerchaResultante = perchaResultante,
            StockBodegaResultante = bodegaResultante,
            NotaExplicativa = "Stock inicial al crear la variante",
            FechaMovimiento = fecha
        };

    private async Task<Error?> ValidarReferenciasAsync(
        int? categoriaId, IReadOnlyList<VarianteNuevaRequest> variantes, CancellationToken ct)
    {
        if (categoriaId is int idCategoria)
        {
            var existe = await db.Set<Categoria>().AnyAsync(c =>
                c.NegocioId == tenant.NegocioId && c.CategoriaId == idCategoria && c.Activo, ct);

            if (!existe)
            {
                return ErroresCatalogo.CategoriaNoEncontrada;
            }
        }

        foreach (var ubicacionId in variantes.Select(v => v.UbicacionBodegaId).OfType<int>().Distinct())
        {
            if (!await UbicacionExisteAsync(ubicacionId, ct))
            {
                return ErroresCatalogo.UbicacionNoEncontrada;
            }
        }

        var skus = variantes
            .Select(v => Atributos.NormalizarSku(v.CodigoSku))
            .OfType<string>()
            .ToList();

        if (skus.Count > 0 && await SkuEnUsoAsync(skus, ct))
        {
            return ErroresCatalogo.SkuDuplicado;
        }

        return null;
    }

    private Task<bool> UbicacionExisteAsync(int ubicacionId, CancellationToken ct) =>
        db.Set<UbicacionesBodega>().AnyAsync(u =>
            u.NegocioId == tenant.NegocioId && u.UbicacionId == ubicacionId && u.Activo, ct);

    private Task<bool> SkuEnUsoAsync(IReadOnlyCollection<string> skus, CancellationToken ct) =>
        db.ProductoVariantes.AnyAsync(v =>
            v.NegocioId == tenant.NegocioId && v.CodigoSku != null && skus.Contains(v.CodigoSku), ct);

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