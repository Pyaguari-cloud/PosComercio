using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Ventas;
using PosComercio.Api.Services.Autorizacion;
using PosComercio.Api.Services.Caja;
using PosComercio.Api.Common.Tiempo;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Services.Negocios;

namespace PosComercio.Api.Services.Ventas;

public interface IVentaService
{
    Task<Resultado<VentaDto>> RegistrarAsync(RegistrarVentaRequest request, CancellationToken ct);
    Task<Resultado<VentaDto>> ObtenerAsync(long ventaId, CancellationToken ct);
    Task<CuadernoDto> ObtenerCuadernoAsync(CuadernoQuery query, CancellationToken ct);
    Task<Resultado<VentaDto>> AnularAsync(long ventaId, AnularVentaRequest request, CancellationToken ct);
}

public sealed class VentaService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IAutorizacionDuenoService autorizacion,
    IConfiguracionNegocioService configuracion,
    TimeProvider tiempo,
    ILogger<VentaService> logger) : IVentaService
{
    // =====================================================================
    // REGISTRAR
    // =====================================================================

    public async Task<Resultado<VentaDto>> RegistrarAsync(RegistrarVentaRequest request, CancellationToken ct)
    {
        // 1. Validaciones sin base de datos
        if (ReglasVenta.Validar(request) is { } errorValidacion)
        {
            return errorValidacion;
        }

        // 2. Idempotencia: si esta venta ya se registró, devolvemos la existente
        if (request.ClientGuid is Guid guid && await BuscarIdPorClientGuidAsync(guid, ct) is long existente)
        {
            return await ObtenerAsync(existente, ct);
        }

        try
        {
            return await RegistrarEnTransaccionAsync(request, ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad() && request.ClientGuid is not null)
        {
            // La misma venta llegó dos veces casi al mismo tiempo
            db.ChangeTracker.Clear();
            var id = await BuscarIdPorClientGuidAsync(request.ClientGuid.Value, ct)
                     ?? throw new InvalidOperationException("Violación de unicidad sin venta existente.", ex);
            return await ObtenerAsync(id, ct);
        }
    }

    private async Task<Resultado<VentaDto>> RegistrarEnTransaccionAsync(
        RegistrarVentaRequest request, CancellationToken ct)
    {
        var negocioId = tenant.NegocioId;
        var ahora = tiempo.GetUtcNow().UtcDateTime;

        // Si salimos de este método sin llegar al Commit, todo se revierte
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // 3. La caja debe estar abierta, y la bloqueamos para que no la cierren a mitad de la venta
        var turno = await db.BloquearTurnoAbiertoAsync(negocioId, ct);
        if (turno is null)
        {
            return ErroresCaja.SinCajaAbierta;
        }

        // 4. Cargar las variantes vendidas (precios actuales y nombre)
        var ids = request.Items.Select(i => i.VarianteId).Distinct().ToList();

        var variantes = await db.ProductoVariantes
            .AsNoTracking()
            .Where(v => v.NegocioId == negocioId && v.Activo && ids.Contains(v.VarianteId))
            .Select(v => new VarianteVendida(
                v.VarianteId,
                v.Producto.Nombre + (v.AtributosResumen != null ? " - " + v.AtributosResumen : ""),
                v.PrecioCosto,
                v.PrecioVentaLista,
                v.PrecioMinimoRegateo))
            .ToDictionaryAsync(v => v.VarianteId, ct);

        var idFaltante = ids.FirstOrDefault(id => !variantes.ContainsKey(id));
        if (idFaltante != 0)
        {
            return ErroresVentas.VarianteNoEncontrada(idFaltante);
        }

        // 5. Si algún precio está bajo el mínimo, se necesita autorización del dueño
        int? autorizadoPor = null;
        var hayPrecioBajoMinimo = request.Items.Any(i =>
            i.PrecioVendidoUnitario < variantes[i.VarianteId].PrecioMinimo);

        if (hayPrecioBajoMinimo)
        {
            var autorizacionResultado = await autorizacion.AutorizarAsync(request.PinAutorizacion, ct);
            if (!autorizacionResultado.Exito)
            {
                return autorizacionResultado.Error;
            }
            autorizadoPor = autorizacionResultado.Valor;
        }

        // 6. Descontar stock. Ordenado por VarianteId para evitar deadlocks.
        var movimientos = new List<MovimientosInventario>();

        foreach (var item in request.Items.OrderBy(i => i.VarianteId))
        {
            var filas = await DescontarStockAsync(item, ct);
            if (filas == 0)
            {
                return ErroresVentas.StockInsuficiente(variantes[item.VarianteId].Nombre, item.SalioDe);
            }

            // Stock exacto después de ESTE descuento, para el kardex
            var stock = await LeerStockAsync(item.VarianteId, ct); ;

            movimientos.Add(new MovimientosInventario
            {
                NegocioId = negocioId,
                VarianteId = item.VarianteId,
                UsuarioId = usuario.UsuarioId,
                TipoMovimiento = TiposMovimientoInventario.Venta,
                UbicacionOrigen = item.SalioDe,
                UbicacionDestino = null,              // sale del local
                Cantidad = item.Cantidad,
                StockPerchaResultante = stock.StockPercha,
                StockBodegaResultante = stock.StockBodega,
                FechaMovimiento = ahora
            });
        }

        // 7. Cabecera. El turno bloqueado hace que las ventas de un negocio vayan
        //    de una en una, así el número correlativo no se repite.
        var numeroVenta = (await db.Ventas
            .Where(v => v.NegocioId == negocioId)
            .MaxAsync(v => (int?)v.NumeroVenta, ct) ?? 0) + 1;

        var venta = new Venta
        {
            NegocioId = negocioId,
            NumeroVenta = numeroVenta,
            UsuarioId = usuario.UsuarioId,
            TurnoId = turno.TurnoId,
            FechaVenta = ahora,
            TotalVenta = ReglasVenta.CalcularTotal(request.Items),
            Estado = EstadosVenta.Completada,
            NotaVenta = request.Nota?.Trim(),
            ClientGuid = request.ClientGuid
        };

        db.Ventas.Add(venta);
        await db.SaveChangesAsync(ct);   // aquí SQL Server asigna el VentaId

        // 8. Detalles, pagos y kardex (ya tenemos el VentaId)
        foreach (var item in request.Items)
        {
            var variante = variantes[item.VarianteId];
            var bajoMinimo = item.PrecioVendidoUnitario < variante.PrecioMinimo;

            db.VentaDetalles.Add(new VentaDetalle
            {
                NegocioId = negocioId,
                VentaId = venta.VentaId,
                VarianteId = item.VarianteId,
                NombreProductoHistorico = variante.Nombre,
                SalioDeUbicacion = item.SalioDe,
                Cantidad = item.Cantidad,
                PrecioCostoHistorico = variante.PrecioCosto,
                PrecioListaUnitario = variante.PrecioLista,
                PrecioMinimoHistorico = variante.PrecioMinimo,
                PrecioVendidoUnitario = item.PrecioVendidoUnitario,
                AutorizadoPorUsuarioId = bajoMinimo ? autorizadoPor : null
            });
        }

        foreach (var pago in request.Pagos)
        {
            db.VentaPagos.Add(new VentaPago
            {
                NegocioId = negocioId,
                VentaId = venta.VentaId,
                UsuarioId = usuario.UsuarioId,
                MetodoPago = pago.MetodoPago,
                Monto = pago.Monto,
                ReferenciaTransferencia = pago.Referencia?.Trim(),
                FechaPago = ahora
            });
        }

        foreach (var movimiento in movimientos)
        {
            movimiento.VentaId = venta.VentaId;
            movimiento.NotaExplicativa = $"Venta #{numeroVenta}";
            db.Set<MovimientosInventario>().Add(movimiento);
        }

        await db.SaveChangesAsync(ct);

        // 9. Todo salió bien: se confirma
        await transaccion.CommitAsync(ct);

        logger.LogInformation("Venta #{NumeroVenta} ({VentaId}) por {Total} registrada por {UsuarioId}",
            numeroVenta, venta.VentaId, venta.TotalVenta, usuario.UsuarioId);

        return await ObtenerAsync(venta.VentaId, ct);
    }

    private Task<StockActual> LeerStockAsync(int varianteId, CancellationToken ct) =>
    db.ProductoVariantes
      .AsNoTracking()
      .Where(v => v.NegocioId == tenant.NegocioId && v.VarianteId == varianteId)
      .Select(v => new StockActual(v.StockPercha, v.StockBodega))
      .FirstAsync(ct);

    /// <summary>
    /// Descuenta stock en UNA sentencia SQL atómica:
    /// UPDATE ... SET StockPercha = StockPercha - @n WHERE ... AND StockPercha >= @n
    /// Si no alcanza, actualiza 0 filas. Nunca deja stock negativo.
    /// </summary>
    private Task<int> DescontarStockAsync(ItemVentaRequest item, CancellationToken ct)
    {
        var variante = db.ProductoVariantes.Where(v =>
            v.NegocioId == tenant.NegocioId && v.VarianteId == item.VarianteId);

        return item.SalioDe == UbicacionesStock.Percha
            ? variante
                .Where(v => v.StockPercha >= item.Cantidad)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockPercha, v => v.StockPercha - item.Cantidad), ct)
            : variante
                .Where(v => v.StockBodega >= item.Cantidad)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockBodega, v => v.StockBodega - item.Cantidad), ct);
    }

    // =====================================================================
    // CONSULTAR
    // =====================================================================

    public async Task<Resultado<VentaDto>> ObtenerAsync(long ventaId, CancellationToken ct)
    {
        var venta = await db.Ventas
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId && v.VentaId == ventaId)
            .Select(AVentaDto(usuario.EsDueno))
            .FirstOrDefaultAsync(ct);

        if (venta is null)
        {
            return ErroresVentas.NoEncontrada;
        }

        return venta;
    }

    // =====================================================================
    // CUADERNO DEL DÍA
    // =====================================================================

    public async Task<CuadernoDto> ObtenerCuadernoAsync(CuadernoQuery query, CancellationToken ct)
    {
        var config = await configuracion.ObtenerAsync(ct);
        var fecha = query.Fecha ?? RangoFechas.HoyLocal(tiempo, config.ZonaHoraria);
        var (desdeUtc, hastaUtc) = RangoFechas.DiaLocalEnUtc(fecha, config.ZonaHoraria);

        var ventasDelDia = db.Ventas
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId
                     && v.FechaVenta >= desdeUtc
                     && v.FechaVenta < hastaUtc);

        if (query.UsuarioId is int usuarioId)
        {
            ventasDelDia = ventasDelDia.Where(v => v.UsuarioId == usuarioId);
        }

        // ---- Totales (solo ventas completadas) ----
        var completadas = ventasDelDia.Where(v => v.Estado == EstadosVenta.Completada);

        var cantidadVentas = await completadas.CountAsync(ct);
        var cantidadAnuladas = await ventasDelDia.CountAsync(v => v.Estado == EstadosVenta.Anulada, ct);
        var totalVendido = await completadas.SumAsync(v => (decimal?)v.TotalVenta, ct) ?? 0m;

        var pagosPorMetodo = await completadas
            .SelectMany(v => v.VentaPagos)
            .GroupBy(p => p.MetodoPago)
            .Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Monto) })
            .ToDictionaryAsync(x => x.Metodo, x => x.Total, ct);

        var detalles = completadas.SelectMany(v => v.VentaDetalles);

        var totalRebajado = await detalles
            .SumAsync(d => (decimal?)d.DescuentoRegateoUnitario * d.Cantidad, ct) ?? 0m;

        decimal? ganancia = null;
        if (usuario.EsDueno)
        {
            ganancia = await detalles.SumAsync(d => (decimal?)d.GananciaLinea, ct) ?? 0m;
        }

        // ---- Listado paginado (incluye anuladas, para que se vean en el cuaderno) ----
        var totalRegistros = await ventasDelDia.CountAsync(ct);

        var items = await ventasDelDia
            .OrderByDescending(v => v.FechaVenta)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .Select(v => new VentaResumenDto(
                v.VentaId,
                v.NumeroVenta,
                v.FechaVenta,
                v.Vendedor.Nombre,
                v.Estado,
                v.TotalVenta,
                v.VentaDetalles.Sum(d => d.Cantidad),
                v.VentaDetalles.Select(d => d.NombreProductoHistorico).ToList(),
                v.VentaPagos.Select(p => p.MetodoPago).ToList()))
            .ToListAsync(ct);

        return new CuadernoDto(
            fecha,
            cantidadVentas,
            cantidadAnuladas,
            totalVendido,
            pagosPorMetodo.GetValueOrDefault(MetodosPago.Efectivo),
            pagosPorMetodo.GetValueOrDefault(MetodosPago.Transferencia),
            pagosPorMetodo.GetValueOrDefault(MetodosPago.Tarjeta),
            totalRebajado,
            ganancia,
            new PaginaDto<VentaResumenDto>(items, query.Pagina, query.TamanoPagina, totalRegistros));
    }

    // =====================================================================
    // ANULAR
    // =====================================================================

    public async Task<Resultado<VentaDto>> AnularAsync(
        long ventaId, AnularVentaRequest request, CancellationToken ct)
    {
        var negocioId = tenant.NegocioId;
        var ahora = tiempo.GetUtcNow().UtcDateTime;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Mismo orden de bloqueo que la venta y el cierre: primero el turno
        var turno = await db.BloquearTurnoAbiertoAsync(negocioId, ct);

        var venta = await db.Ventas
            .AsNoTracking()
            .Where(v => v.NegocioId == negocioId && v.VentaId == ventaId)
            .Select(v => new { v.NumeroVenta, v.Estado, v.TurnoId })
            .FirstOrDefaultAsync(ct);

        // Validaciones de estado, de la más simple a la más específica
        if (venta is null)
        {
            return ErroresVentas.NoEncontrada;
        }

        if (venta.Estado == EstadosVenta.Anulada)
        {
            return ErroresVentas.YaAnulada;
        }

        if (turno is null || venta.TurnoId != turno.TurnoId)
        {
            return ErroresVentas.FueraDeTurno;
        }

        // La autorización va DESPUÉS de las validaciones: no gastamos un intento
        // de PIN en una venta que de todas formas no se puede anular
        var autorizacionResultado = await autorizacion.AutorizarAsync(request.PinAutorizacion, ct);
        if (!autorizacionResultado.Exito)
        {
            return autorizacionResultado.Error;
        }

        var motivo = request.Motivo.Trim();

        // Marcar como anulada. El Where con "COMPLETADA" protege contra dos anulaciones simultáneas.
        var filas = await db.Ventas
            .Where(v => v.NegocioId == negocioId
                     && v.VentaId == ventaId
                     && v.Estado == EstadosVenta.Completada)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.Estado, EstadosVenta.Anulada)
                .SetProperty(v => v.FechaAnulacion, (DateTime?)ahora)
                .SetProperty(v => v.UsuarioAnulaId, (int?)autorizacionResultado.Valor)
                .SetProperty(v => v.MotivoAnulacion, motivo), ct);

        if (filas == 0)
        {
            return ErroresVentas.YaAnulada;
        }

        // Devolver el stock a donde salió, en orden de VarianteId (evita deadlocks)
        var detalles = await db.VentaDetalles
            .AsNoTracking()
            .Where(d => d.NegocioId == negocioId && d.VentaId == ventaId)
            .OrderBy(d => d.VarianteId)
            .Select(d => new { d.VarianteId, d.Cantidad, d.SalioDeUbicacion })
            .ToListAsync(ct);

        foreach (var detalle in detalles)
        {
            await DevolverStockAsync(detalle.VarianteId, detalle.SalioDeUbicacion, detalle.Cantidad, ct);
            var stock = await LeerStockAsync(detalle.VarianteId, ct);

            db.Set<MovimientosInventario>().Add(new MovimientosInventario
            {
                NegocioId = negocioId,
                VarianteId = detalle.VarianteId,
                UsuarioId = usuario.UsuarioId,
                TipoMovimiento = TiposMovimientoInventario.Anulacion,
                UbicacionOrigen = null,                       // entra al local
                UbicacionDestino = detalle.SalioDeUbicacion,  // vuelve a donde salió
                Cantidad = detalle.Cantidad,
                StockPerchaResultante = stock.StockPercha,
                StockBodegaResultante = stock.StockBodega,
                VentaId = ventaId,
                NotaExplicativa = $"Anulación de la venta #{venta.NumeroVenta}: {motivo}",
                FechaMovimiento = ahora
            });
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        logger.LogWarning(
            "Venta #{NumeroVenta} ({VentaId}) anulada por {UsuarioId}, autorizó {AutorizadoPor}. Motivo: {Motivo}",
            venta.NumeroVenta, ventaId, usuario.UsuarioId, autorizacionResultado.Valor, motivo);

        return await ObtenerAsync(ventaId, ct);
    }

    private Task<int> DevolverStockAsync(int varianteId, string ubicacion, int cantidad, CancellationToken ct)
    {
        var variante = db.ProductoVariantes.Where(v =>
            v.NegocioId == tenant.NegocioId && v.VarianteId == varianteId);

        return ubicacion == UbicacionesStock.Percha
            ? variante.ExecuteUpdateAsync(s => s.SetProperty(v => v.StockPercha, v => v.StockPercha + cantidad), ct)
            : variante.ExecuteUpdateAsync(s => s.SetProperty(v => v.StockBodega, v => v.StockBodega + cantidad), ct);
    }

    private Task<long?> BuscarIdPorClientGuidAsync(Guid clientGuid, CancellationToken ct) =>
        db.Ventas
          .AsNoTracking()
          .Where(v => v.NegocioId == tenant.NegocioId && v.ClientGuid == clientGuid)
          .Select(v => (long?)v.VentaId)
          .FirstOrDefaultAsync(ct);

    private static Expression<Func<Venta, VentaDto>> AVentaDto(bool verGanancia) => v =>
        new VentaDto(
            v.VentaId,
            v.NumeroVenta,
            v.FechaVenta,
            v.Vendedor.Nombre,
            v.Estado,
            v.TotalVenta,
            v.NotaVenta,
            v.VentaDetalles
                .OrderBy(d => d.VentaDetalleId)
                .Select(d => new VentaDetalleDto(
                    d.VentaDetalleId,
                    d.VarianteId,
                    d.NombreProductoHistorico,
                    d.SalioDeUbicacion,
                    d.Cantidad,
                    d.PrecioListaUnitario,
                    d.PrecioVendidoUnitario,
                    (decimal?)d.DescuentoRegateoUnitario ?? 0,
                    (decimal?)d.Subtotal ?? 0,
                    d.AutorizadoPor != null ? d.AutorizadoPor.Nombre : null))
                .ToList(),
            v.VentaPagos
                .Select(p => new VentaPagoDto(p.MetodoPago, p.Monto, p.ReferenciaTransferencia))
                .ToList(),
            v.VentaDetalles.Sum(d => (decimal?)d.DescuentoRegateoUnitario * d.Cantidad) ?? 0,
            verGanancia ? v.VentaDetalles.Sum(d => (decimal?)d.GananciaLinea) : null,
            v.MotivoAnulacion,
            v.UsuarioAnula != null ? v.UsuarioAnula.Nombre : null);
}

/// <summary>Datos de una variante al momento de la venta (para no repetir consultas).</summary>
internal sealed record VarianteVendida(
    int VarianteId,
    string Nombre,
    decimal PrecioCosto,
    decimal PrecioLista,
    decimal PrecioMinimo);

internal sealed record StockActual(int StockPercha, int StockBodega);