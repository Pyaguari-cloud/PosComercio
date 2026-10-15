using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Inventario;
using PosComercio.Api.Services.Autorizacion;
using PosComercio.Api.Services.Catalogo;

namespace PosComercio.Api.Services.Inventario;

public interface IInventarioService
{
    Task<Resultado<MovimientoRealizadoDto>> TraspasarAsync(TraspasoRequest request, CancellationToken ct);
    Task<Resultado<CompraRealizadaDto>> RegistrarCompraAsync(CompraRequest request, CancellationToken ct);
    Task<Resultado<MovimientoRealizadoDto>> RegistrarAjusteAsync(AjusteRequest request, CancellationToken ct);
    Task<Resultado<ConteoResultadoDto>> RegistrarConteoAsync(ConteoRequest request, CancellationToken ct);
    Task<IReadOnlyList<ReponerPerchaItemDto>> ListarReponerPerchaAsync(CancellationToken ct);
    Task<IReadOnlyList<StockBajoItemDto>> ListarStockBajoAsync(CancellationToken ct);
    Task<Resultado<PaginaDto<KardexItemDto>>> ObtenerKardexAsync(int varianteId, PaginacionQuery query, CancellationToken ct);
}

public sealed class InventarioService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IKardexService kardex,
    IAutorizacionDuenoService autorizacion,
    ILogger<InventarioService> logger) : IInventarioService
{
    // =====================================================================
    // TRASPASOS (percha ⇄ bodega)
    // =====================================================================

    public async Task<Resultado<MovimientoRealizadoDto>> TraspasarAsync(TraspasoRequest request, CancellationToken ct)
    {
        if (request.ClientGuid is Guid guid && await BuscarPorClientGuidAsync(guid, ct) is { } yaRegistrado)
        {
            return yaRegistrado;
        }

        var (tipo, origen, destino) = request.Direccion == DireccionesTraspaso.APercha
            ? (TiposMovimientoInventario.PasarAPercha, UbicacionesStock.Bodega, UbicacionesStock.Percha)
            : (TiposMovimientoInventario.DevolverABodega, UbicacionesStock.Percha, UbicacionesStock.Bodega);

        var movimiento = new MovimientoStock(
            request.VarianteId, tipo, origen, destino, request.Cantidad,
            Atributos.Limpiar(request.Nota), request.ClientGuid);

        try
        {
            return await EjecutarUnoAsync(movimiento, ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad() && request.ClientGuid is not null)
        {
            db.ChangeTracker.Clear();
            return await BuscarPorClientGuidAsync(request.ClientGuid.Value, ct)
                   ?? throw new InvalidOperationException("Violación de unicidad sin movimiento existente.", ex);
        }
    }

    // =====================================================================
    // COMPRAS (entrada de mercadería)
    // =====================================================================

    public async Task<Resultado<CompraRealizadaDto>> RegistrarCompraAsync(CompraRequest request, CancellationToken ct)
    {
        if (request.Items.GroupBy(i => (i.VarianteId, i.Destino)).Any(g => g.Count() > 1))
        {
            return ErroresInventario.ItemsRepetidos;
        }

        if (request.Items.Any(i => i.CostoUnitario is decimal c && decimal.Round(c, 2) != c))
        {
            return ErroresCatalogo.MasDeDosDecimales;
        }

        var ids = request.Items.Select(i => i.VarianteId).Distinct().ToList();
        var nombres = await NombresVariantesAsync(ids, ct);

        if (ids.Any(id => !nombres.ContainsKey(id)))
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        var nota = Atributos.Limpiar(request.Proveedor) is { } proveedor
            ? $"Compra a {proveedor}"
            : "Compra de mercadería";

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var registros = new List<(MovimientosInventario Registro, string Producto)>();

        foreach (var item in request.Items.OrderBy(i => i.VarianteId))
        {
            if (item.CostoUnitario is decimal costo)
            {
                await db.ProductoVariantes
                    .Where(v => v.NegocioId == tenant.NegocioId && v.VarianteId == item.VarianteId)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.PrecioCosto, costo), ct);
            }

            // Una compra solo SUMA: no puede fallar por falta de stock
            var registro = await kardex.AplicarAsync(
                new MovimientoStock(item.VarianteId, TiposMovimientoInventario.Compra,
                    Origen: null, Destino: item.Destino, item.Cantidad, nota), ct)
                ?? throw new InvalidOperationException("Una compra no debería fallar por stock.");

            registros.Add((registro, nombres[item.VarianteId]));
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var unidades = registros.Sum(r => r.Registro.Cantidad);
        logger.LogInformation("Compra registrada por {UsuarioId}: {Items} producto(s), {Unidades} unidad(es). {Nota}",
            usuario.UsuarioId, registros.Count, unidades, nota);

        return new CompraRealizadaDto(
            registros.Count,
            unidades,
            registros.Select(r => ADto(r.Registro, r.Producto)).ToList());
    }

    // =====================================================================
    // AJUSTES (pérdida, daño, hallazgo)
    // =====================================================================

    public async Task<Resultado<MovimientoRealizadoDto>> RegistrarAjusteAsync(AjusteRequest request, CancellationToken ct)
    {
        var autorizacionResultado = await autorizacion.AutorizarAsync(request.PinAutorizacion, ct);
        if (!autorizacionResultado.Exito)
        {
            return autorizacionResultado.Error;
        }

        // Pérdida y daño SACAN stock; encontrado lo AGREGA
        var esSalida = request.Tipo is TiposMovimientoInventario.Perdida or TiposMovimientoInventario.Danado;

        var movimiento = new MovimientoStock(
            request.VarianteId,
            request.Tipo,
            Origen: esSalida ? request.Ubicacion : null,
            Destino: esSalida ? null : request.Ubicacion,
            request.Cantidad,
            request.Nota.Trim());

        var resultado = await EjecutarUnoAsync(movimiento, ct);

        if (resultado.Exito)
        {
            logger.LogWarning(
                "Ajuste {Tipo} de {Cantidad} en la variante {VarianteId} por {UsuarioId}, autorizó {DuenoId}: {Nota}",
                request.Tipo, request.Cantidad, request.VarianteId, usuario.UsuarioId,
                autorizacionResultado.Valor, request.Nota);
        }

        return resultado;
    }

    // =====================================================================
    // CONTEO FÍSICO
    // =====================================================================

    public async Task<Resultado<ConteoResultadoDto>> RegistrarConteoAsync(ConteoRequest request, CancellationToken ct)
    {
        var producto = await NombreVarianteAsync(request.VarianteId, ct);
        if (producto is null)
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Bloquea la fila: mientras ajustamos, nadie vende ni mueve esta variante
        var actual = await db.ProductoVariantes
            .FromSql($"""
                SELECT * FROM dbo.ProductoVariantes WITH (UPDLOCK, ROWLOCK)
                WHERE NegocioId = {tenant.NegocioId} AND VarianteId = {request.VarianteId}
                """)
            .AsNoTracking()
            .Select(v => new { v.StockPercha, v.StockBodega, v.PrecioCosto })
            .FirstAsync(ct);

        var diferenciaPercha = request.ConteoPercha - actual.StockPercha;
        var diferenciaBodega = request.ConteoBodega - actual.StockBodega;
        var nota = Atributos.Limpiar(request.Nota) ?? "Conteo físico";

        foreach (var (ubicacion, diferencia) in new[]
                 {
                     (UbicacionesStock.Percha, diferenciaPercha),
                     (UbicacionesStock.Bodega, diferenciaBodega)
                 })
        {
            if (diferencia == 0)
            {
                continue;
            }

            // Si se contó MÁS de lo registrado, entra; si se contó MENOS, sale
            var movimiento = diferencia > 0
                ? new MovimientoStock(request.VarianteId, TiposMovimientoInventario.AjusteConteo,
                    Origen: null, Destino: ubicacion, diferencia, nota)
                : new MovimientoStock(request.VarianteId, TiposMovimientoInventario.AjusteConteo,
                    Origen: ubicacion, Destino: null, -diferencia, nota);

            _ = await kardex.AplicarAsync(movimiento, ct)
                ?? throw new InvalidOperationException("La fila está bloqueada; el ajuste del conteo no puede fallar.");
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var diferenciaTotal = diferenciaPercha + diferenciaBodega;
        var valorDiferencia = diferenciaTotal * actual.PrecioCosto;

        if (diferenciaTotal != 0)
        {
            logger.LogWarning("Conteo de la variante {VarianteId} por {UsuarioId}: diferencia {Diferencia} unidades ({Valor})",
                request.VarianteId, usuario.UsuarioId, diferenciaTotal, valorDiferencia);
        }

        return new ConteoResultadoDto(
            request.VarianteId, producto,
            actual.StockPercha, actual.StockBodega,
            request.ConteoPercha, request.ConteoBodega,
            diferenciaPercha, diferenciaBodega,
            valorDiferencia);
    }

    // =====================================================================
    // CONSULTAS
    // =====================================================================

    public async Task<IReadOnlyList<ReponerPerchaItemDto>> ListarReponerPerchaAsync(CancellationToken ct) =>
        await db.ProductoVariantes
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId
                     && v.Activo
                     && v.Producto.Activo
                     && v.StockPercha == 0
                     && v.StockBodega > 0)
            .OrderBy(v => v.Producto.Nombre)
            .ThenBy(v => v.Talla)
            .Select(v => new ReponerPerchaItemDto(
                v.VarianteId,
                v.ProductoId,
                v.Producto.Nombre,
                v.AtributosResumen,
                v.StockBodega,
                v.UbicacionesBodega != null ? v.UbicacionesBodega.Nombre : null,
                v.ReferenciaBodega,
                v.ReferenciaPercha))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StockBajoItemDto>> ListarStockBajoAsync(CancellationToken ct) =>
        await db.ProductoVariantes
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId
                     && v.Activo
                     && v.Producto.Activo
                     && v.StockMinimoAlerta > 0                                // 0 = "no me avises"
                     && v.StockPercha + v.StockBodega <= v.StockMinimoAlerta)
            .OrderBy(v => v.StockPercha + v.StockBodega)                       // lo más urgente primero
            .ThenBy(v => v.Producto.Nombre)
            .Select(v => new StockBajoItemDto(
                v.VarianteId,
                v.ProductoId,
                v.Producto.Nombre,
                v.AtributosResumen,
                v.StockPercha,
                v.StockBodega,
                v.StockMinimoAlerta))
            .ToListAsync(ct);

    public async Task<Resultado<PaginaDto<KardexItemDto>>> ObtenerKardexAsync(
        int varianteId, PaginacionQuery query, CancellationToken ct)
    {
        if (await NombreVarianteAsync(varianteId, ct) is null)
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        var consulta = db.Set<MovimientosInventario>()
            .AsNoTracking()
            .Where(m => m.NegocioId == tenant.NegocioId && m.VarianteId == varianteId);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(m => m.MovimientoId)                            // lo más reciente primero
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .Select(m => new KardexItemDto(
                m.MovimientoId,
                m.FechaMovimiento,
                m.TipoMovimiento,
                m.UbicacionOrigen,
                m.UbicacionDestino,
                m.Cantidad,
                m.StockPerchaResultante,
                m.StockBodegaResultante,
                m.Usuario.Nombre,
                m.NotaExplicativa,
                m.VentaId))
            .ToListAsync(ct);

        return new PaginaDto<KardexItemDto>(items, query.Pagina, query.TamanoPagina, total);
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    /// <summary>Ejecuta UN movimiento en su propia transacción.</summary>
    private async Task<Resultado<MovimientoRealizadoDto>> EjecutarUnoAsync(MovimientoStock movimiento, CancellationToken ct)
    {
        var producto = await NombreVarianteAsync(movimiento.VarianteId, ct);
        if (producto is null)
        {
            return ErroresCatalogo.VarianteNoEncontrada;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var registro = await kardex.AplicarAsync(movimiento, ct);
        if (registro is null)
        {
            // Solo puede fallar por stock, así que aquí siempre hay un origen
            return ErroresInventario.StockInsuficiente(producto, movimiento.Origen!);
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        logger.LogInformation("Movimiento {Tipo} de {Cantidad} en la variante {VarianteId} por {UsuarioId}",
            movimiento.Tipo, movimiento.Cantidad, movimiento.VarianteId, usuario.UsuarioId);

        return ADto(registro, producto);
    }

    private Task<string?> NombreVarianteAsync(int varianteId, CancellationToken ct) =>
        db.ProductoVariantes
          .AsNoTracking()
          .Where(v => v.NegocioId == tenant.NegocioId && v.VarianteId == varianteId)
          .Select(v => v.Producto.Nombre + (v.AtributosResumen != null ? " - " + v.AtributosResumen : ""))
          .FirstOrDefaultAsync(ct);

    private Task<Dictionary<int, string>> NombresVariantesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        db.ProductoVariantes
          .AsNoTracking()
          .Where(v => v.NegocioId == tenant.NegocioId && ids.Contains(v.VarianteId))
          .Select(v => new
          {
              v.VarianteId,
              Nombre = v.Producto.Nombre + (v.AtributosResumen != null ? " - " + v.AtributosResumen : "")
          })
          .ToDictionaryAsync(x => x.VarianteId, x => x.Nombre, ct);

    private Task<MovimientoRealizadoDto?> BuscarPorClientGuidAsync(Guid clientGuid, CancellationToken ct) =>
        db.Set<MovimientosInventario>()
          .AsNoTracking()
          .Where(m => m.NegocioId == tenant.NegocioId && m.ClientGuid == clientGuid)
          .Select(m => new MovimientoRealizadoDto(
              m.MovimientoId,
              m.VarianteId,
              m.Variante.Producto.Nombre + (m.Variante.AtributosResumen != null ? " - " + m.Variante.AtributosResumen : ""),
              m.TipoMovimiento,
              m.Cantidad,
              m.StockPerchaResultante,
              m.StockBodegaResultante))
          .FirstOrDefaultAsync(ct);

    private static MovimientoRealizadoDto ADto(MovimientosInventario registro, string producto) => new(
        registro.MovimientoId,
        registro.VarianteId,
        producto,
        registro.TipoMovimiento,
        registro.Cantidad,
        registro.StockPerchaResultante,
        registro.StockBodegaResultante);
}