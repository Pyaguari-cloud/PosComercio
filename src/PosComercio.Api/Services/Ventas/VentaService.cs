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

namespace PosComercio.Api.Services.Ventas;

public interface IVentaService
{
    Task<Resultado<VentaDto>> RegistrarAsync(RegistrarVentaRequest request, CancellationToken ct);
    Task<Resultado<VentaDto>> ObtenerAsync(long ventaId, CancellationToken ct);
}

public sealed class VentaService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IAutorizacionDuenoService autorizacion,
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
            var stock = await db.ProductoVariantes
                .AsNoTracking()
                .Where(v => v.NegocioId == negocioId && v.VarianteId == item.VarianteId)
                .Select(v => new { v.StockPercha, v.StockBodega })
                .FirstAsync(ct);

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
            verGanancia ? v.VentaDetalles.Sum(d => (decimal?)d.GananciaLinea) : null);
}

/// <summary>Datos de una variante al momento de la venta (para no repetir consultas).</summary>
internal sealed record VarianteVendida(
    int VarianteId,
    string Nombre,
    decimal PrecioCosto,
    decimal PrecioLista,
    decimal PrecioMinimo);