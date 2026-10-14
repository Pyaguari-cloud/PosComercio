using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Caja;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Services.Negocios;

namespace PosComercio.Api.Services.Caja;

public interface ICajaService
{
    Task<EstadoCajaDto> ObtenerEstadoAsync(CancellationToken ct);
    Task<Resultado<EstadoCajaDto>> AbrirAsync(AbrirCajaRequest request, CancellationToken ct);
    Task<Resultado<MovimientoCajaDto>> RegistrarMovimientoAsync(RegistrarMovimientoCajaRequest request, CancellationToken ct);
    Task<Resultado<CierreCajaDto>> CerrarAsync(CerrarCajaRequest request, CancellationToken ct);
    Task<PaginaDto<CierreCajaDto>> ListarCierresAsync(PaginacionQuery query, CancellationToken ct);
}

public sealed class CajaService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    IConfiguracionNegocioService configuracion,
    TimeProvider tiempo,
    ILogger<CajaService> logger) : ICajaService
{
    private DateTime Ahora => tiempo.GetUtcNow().UtcDateTime;

    // =====================================================================
    // CONSULTAR
    // =====================================================================

    public async Task<EstadoCajaDto> ObtenerEstadoAsync(CancellationToken ct)
    {
        var config = await configuracion.ObtenerAsync(ct);
        var turno = await BuscarTurnoAbiertoAsync(ct);

        var turnoDto = turno is null ? null : await ConstruirTurnoActualAsync(turno, config, ct);

        return new EstadoCajaDto(turno is not null, config.CierreCiego, config.MontoInicialSugerido, turnoDto);
    }

    // =====================================================================
    // ABRIR
    // =====================================================================

    public async Task<Resultado<EstadoCajaDto>> AbrirAsync(AbrirCajaRequest request, CancellationToken ct)
    {
        // Validación "amable": da un buen mensaje en el caso normal
        if (await BuscarTurnoAbiertoAsync(ct) is not null)
        {
            return ErroresCaja.CajaYaAbierta;
        }

        var turno = new CajaTurno
        {
            NegocioId = tenant.NegocioId,
            UsuarioAbreId = usuario.UsuarioId,
            MontoInicial = request.MontoInicial,
            Estado = EstadosTurno.Abierto,
            FechaApertura = Ahora
        };

        db.CajaTurnos.Add(turno);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            // Protección "real": dos personas abrieron caja al mismo tiempo
            return ErroresCaja.CajaYaAbierta;
        }

        logger.LogInformation("Caja {TurnoId} abierta por {UsuarioId} con {MontoInicial}",
            turno.TurnoId, usuario.UsuarioId, turno.MontoInicial);

        return await ObtenerEstadoAsync(ct);
    }

    // =====================================================================
    // GASTOS, RETIROS E INGRESOS
    // =====================================================================

    public async Task<Resultado<MovimientoCajaDto>> RegistrarMovimientoAsync(
        RegistrarMovimientoCajaRequest request, CancellationToken ct)
    {
        if (request.Tipo == TiposMovimientoCaja.Retiro && !usuario.EsDueno)
        {
            return ErroresCaja.RetiroSoloDueno;
        }

        // Idempotencia: si este mismo movimiento ya llegó antes, devolvemos el existente
        if (request.ClientGuid is Guid guid &&
            await BuscarMovimientoPorGuidAsync(guid, ct) is { } yaRegistrado)
        {
            return yaRegistrado;
        }

        var turno = await BuscarTurnoAbiertoAsync(ct);
        if (turno is null)
        {
            return ErroresCaja.SinCajaAbierta;
        }

        if (request.Tipo is TiposMovimientoCaja.Gasto or TiposMovimientoCaja.Retiro)
        {
            var resumen = await CalcularResumenAsync(turno.TurnoId, ct);
            if (request.Monto > resumen.EfectivoEsperado(turno.MontoInicial))
            {
                return ErroresCaja.EfectivoInsuficiente;
            }
        }

        var movimiento = new CajaMovimiento
        {
            NegocioId = tenant.NegocioId,
            TurnoId = turno.TurnoId,
            UsuarioId = usuario.UsuarioId,
            Tipo = request.Tipo,
            Monto = request.Monto,
            Concepto = request.Concepto.Trim(),
            Fecha = Ahora,
            ClientGuid = request.ClientGuid
        };

        db.CajaMovimientos.Add(movimiento);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad() && request.ClientGuid is not null)
        {
            // El mismo movimiento llegó dos veces casi al mismo tiempo: devolvemos el que quedó guardado
            db.ChangeTracker.Clear();
            return await BuscarMovimientoPorGuidAsync(request.ClientGuid.Value, ct)
                   ?? throw new InvalidOperationException("Violación de unicidad sin movimiento existente.", ex);
        }

        logger.LogInformation("Movimiento de caja {Tipo} por {Monto} en el turno {TurnoId}",
            movimiento.Tipo, movimiento.Monto, turno.TurnoId);

        return new MovimientoCajaDto(
            movimiento.CajaMovimientoId, movimiento.Tipo, movimiento.Monto,
            movimiento.Concepto, movimiento.Fecha, usuario.Nombre);
    }

    // =====================================================================
    // CERRAR
    // =====================================================================

    public async Task<Resultado<CierreCajaDto>> CerrarAsync(CerrarCajaRequest request, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Bloquea la fila del turno hasta el final de la transacción.
        // En la Fase 5, las ventas también leerán el turno con este bloqueo:
        // así, una venta y un cierre nunca ocurren "a la vez".
        var turno = await db.CajaTurnos
            .FromSql($"""
                SELECT * FROM dbo.CajaTurnos WITH (UPDLOCK, ROWLOCK)
                WHERE NegocioId = {tenant.NegocioId} AND Estado = {EstadosTurno.Abierto}
                """)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (turno is null)
        {
            return ErroresCaja.SinCajaAbierta;
        }

        var resumen = await CalcularResumenAsync(turno.TurnoId, ct);
        var efectivoEsperado = resumen.EfectivoEsperado(turno.MontoInicial);
        var ahora = Ahora;

        // UPDATE directo en SQL, con la condición "todavía está ABIERTO"
        var filasActualizadas = await db.CajaTurnos
            .Where(t => t.NegocioId == tenant.NegocioId
                     && t.TurnoId == turno.TurnoId
                     && t.Estado == EstadosTurno.Abierto)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Estado, EstadosTurno.Cerrado)
                .SetProperty(t => t.UsuarioCierraId, (int?)usuario.UsuarioId)
                .SetProperty(t => t.FechaCierre, (DateTime?)ahora)
                .SetProperty(t => t.TotalVentas, (decimal?)resumen.TotalVentas)
                .SetProperty(t => t.TotalEfectivoVentas, (decimal?)resumen.Efectivo)
                .SetProperty(t => t.TotalTransferencias, (decimal?)resumen.Transferencias)
                .SetProperty(t => t.TotalTarjeta, (decimal?)resumen.Tarjeta)
                .SetProperty(t => t.TotalGastosRetiros, (decimal?)resumen.GastosRetiros)
                .SetProperty(t => t.TotalIngresosExtra, (decimal?)resumen.IngresosExtra)
                .SetProperty(t => t.GananciaDia, (decimal?)resumen.Ganancia)
                .SetProperty(t => t.EfectivoEsperado, (decimal?)efectivoEsperado)
                .SetProperty(t => t.EfectivoContado, (decimal?)request.EfectivoContado)
                .SetProperty(t => t.Nota, request.Nota), ct);

        if (filasActualizadas == 0)
        {
            return ErroresCaja.CajaYaCerrada;
        }

        await transaccion.CommitAsync(ct);

        logger.LogInformation(
            "Caja {TurnoId} cerrada por {UsuarioId}. Esperado {Esperado}, contado {Contado}, diferencia {Diferencia}",
            turno.TurnoId, usuario.UsuarioId, efectivoEsperado, request.EfectivoContado,
            request.EfectivoContado - efectivoEsperado);

        return await db.CajaTurnos
            .AsNoTracking()
            .Where(t => t.NegocioId == tenant.NegocioId && t.TurnoId == turno.TurnoId)
            .Select(ACierreDto(usuario.EsDueno))
            .FirstAsync(ct);
    }

    // =====================================================================
    // HISTORIAL
    // =====================================================================

    public async Task<PaginaDto<CierreCajaDto>> ListarCierresAsync(PaginacionQuery query, CancellationToken ct)
    {
        var consulta = db.CajaTurnos
            .AsNoTracking()
            .Where(t => t.NegocioId == tenant.NegocioId && t.Estado == EstadosTurno.Cerrado);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(t => t.FechaCierre)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .Select(ACierreDto(usuario.EsDueno))
            .ToListAsync(ct);

        return new PaginaDto<CierreCajaDto>(items, query.Pagina, query.TamanoPagina, total);
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private Task<CajaTurno?> BuscarTurnoAbiertoAsync(CancellationToken ct) =>
        db.CajaTurnos
          .AsNoTracking()
          .FirstOrDefaultAsync(t => t.NegocioId == tenant.NegocioId && t.Estado == EstadosTurno.Abierto, ct);

    private async Task<TurnoActualDto> ConstruirTurnoActualAsync(
        CajaTurno turno, ConfiguracionNegocio config, CancellationToken ct)
    {
        var resumen = await CalcularResumenAsync(turno.TurnoId, ct);

        var abiertoPor = await db.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioId == turno.UsuarioAbreId)
            .Select(u => u.Nombre)
            .FirstAsync(ct);

        var movimientos = await db.CajaMovimientos
            .AsNoTracking()
            .Where(m => m.NegocioId == tenant.NegocioId && m.TurnoId == turno.TurnoId)
            .OrderByDescending(m => m.Fecha)
            .Select(AMovimientoDto)
            .ToListAsync(ct);

        // Cierre ciego: el vendedor no ve cuánto debería haber
        var puedeVerEsperado = usuario.EsDueno || !config.CierreCiego;

        return new TurnoActualDto(
            turno.TurnoId,
            turno.FechaApertura,
            abiertoPor,
            turno.MontoInicial,
            resumen.CantidadVentas,
            resumen.TotalVentas,
            resumen.Efectivo,
            resumen.Transferencias,
            resumen.Tarjeta,
            resumen.GastosRetiros,
            resumen.IngresosExtra,
            puedeVerEsperado ? resumen.EfectivoEsperado(turno.MontoInicial) : null,
            usuario.EsDueno ? resumen.Ganancia : null,
            movimientos);
    }

    /// <summary>Calcula los totales de un turno a partir de ventas, pagos y movimientos.</summary>
    private async Task<ResumenCaja> CalcularResumenAsync(long turnoId, CancellationToken ct)
    {
        var negocioId = tenant.NegocioId;

        var ventas = db.Ventas.AsNoTracking().Where(v =>
            v.NegocioId == negocioId && v.TurnoId == turnoId && v.Estado == EstadosVenta.Completada);

        var cantidadVentas = await ventas.CountAsync(ct);
        var totalVentas = await ventas.SumAsync(v => (decimal?)v.TotalVenta, ct) ?? 0m;

        var pagosPorMetodo = await db.VentaPagos
            .AsNoTracking()
            .Where(p => p.NegocioId == negocioId
                     && p.Venta.TurnoId == turnoId
                     && p.Venta.Estado == EstadosVenta.Completada)
            .GroupBy(p => p.MetodoPago)
            .Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Monto) })
            .ToDictionaryAsync(x => x.Metodo, x => x.Total, ct);

        var ganancia = await db.VentaDetalles
            .AsNoTracking()
            .Where(d => d.NegocioId == negocioId
                     && d.Venta.TurnoId == turnoId
                     && d.Venta.Estado == EstadosVenta.Completada)
            .SumAsync(d => (decimal?)d.GananciaLinea, ct) ?? 0m;

        var movimientosPorTipo = await db.CajaMovimientos
            .AsNoTracking()
            .Where(m => m.NegocioId == negocioId && m.TurnoId == turnoId)
            .GroupBy(m => m.Tipo)
            .Select(g => new { Tipo = g.Key, Total = g.Sum(m => m.Monto) })
            .ToDictionaryAsync(x => x.Tipo, x => x.Total, ct);

        return new ResumenCaja(
            CantidadVentas: cantidadVentas,
            TotalVentas: totalVentas,
            Efectivo: pagosPorMetodo.GetValueOrDefault(MetodosPago.Efectivo),
            Transferencias: pagosPorMetodo.GetValueOrDefault(MetodosPago.Transferencia),
            Tarjeta: pagosPorMetodo.GetValueOrDefault(MetodosPago.Tarjeta),
            GastosRetiros: movimientosPorTipo.GetValueOrDefault(TiposMovimientoCaja.Gasto)
                         + movimientosPorTipo.GetValueOrDefault(TiposMovimientoCaja.Retiro),
            IngresosExtra: movimientosPorTipo.GetValueOrDefault(TiposMovimientoCaja.IngresoExtra),
            Ganancia: ganancia);
    }

    private Task<MovimientoCajaDto?> BuscarMovimientoPorGuidAsync(Guid clientGuid, CancellationToken ct) =>
        db.CajaMovimientos
          .AsNoTracking()
          .Where(m => m.NegocioId == tenant.NegocioId && m.ClientGuid == clientGuid)
          .Select(AMovimientoDto)
          .FirstOrDefaultAsync(ct);

    private static readonly Expression<Func<CajaMovimiento, MovimientoCajaDto>> AMovimientoDto = m =>
        new MovimientoCajaDto(m.CajaMovimientoId, m.Tipo, m.Monto, m.Concepto, m.Fecha, m.Usuario.Nombre);

    private static Expression<Func<CajaTurno, CierreCajaDto>> ACierreDto(bool verGanancia) => t =>
        new CierreCajaDto(
            t.TurnoId,
            t.FechaApertura,
            t.FechaCierre,
            t.UsuarioAbre.Nombre,
            t.UsuarioCierra != null ? t.UsuarioCierra.Nombre : null,
            t.MontoInicial,
            t.TotalVentas ?? 0,
            t.TotalEfectivoVentas ?? 0,
            t.TotalTransferencias ?? 0,
            t.TotalTarjeta ?? 0,
            t.TotalGastosRetiros ?? 0,
            t.TotalIngresosExtra ?? 0,
            t.EfectivoEsperado ?? 0,
            t.EfectivoContado ?? 0,
            t.Diferencia ?? 0,
            verGanancia ? t.GananciaDia : null,
            t.Nota);
}

/// <summary>Totales de un turno. La fórmula del efectivo esperado vive en UN solo lugar.</summary>
internal sealed record ResumenCaja(
    int CantidadVentas,
    decimal TotalVentas,
    decimal Efectivo,
    decimal Transferencias,
    decimal Tarjeta,
    decimal GastosRetiros,
    decimal IngresosExtra,
    decimal Ganancia)
{
    public decimal EfectivoEsperado(decimal montoInicial) =>
        montoInicial + Efectivo + IngresosExtra - GastosRetiros;
}