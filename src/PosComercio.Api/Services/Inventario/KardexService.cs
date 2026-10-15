using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Common.Tenancy;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Services.Inventario;

/// <summary>
/// Describe un movimiento de stock.
/// Origen null = entra desde afuera. Destino null = sale del local.
/// </summary>
public sealed record MovimientoStock(
    int VarianteId,
    string Tipo,
    string? Origen,
    string? Destino,
    int Cantidad,
    string? Nota = null,
    Guid? ClientGuid = null);

public interface IKardexService
{
    /// <summary>
    /// Actualiza el stock de forma atómica y agrega el registro al kardex (sin guardar).
    /// Devuelve null si no hay stock suficiente en el origen.
    /// Debe llamarse DENTRO de una transacción, y el llamador hace SaveChanges.
    /// </summary>
    Task<MovimientosInventario?> AplicarAsync(MovimientoStock movimiento, CancellationToken ct);
}

public sealed class KardexService(
    PosComercioDbContext db,
    ITenantContext tenant,
    IUsuarioActual usuario,
    TimeProvider tiempo) : IKardexService
{
    public async Task<MovimientosInventario?> AplicarAsync(MovimientoStock m, CancellationToken ct)
    {
        // Cláusulas de guarda: errores de PROGRAMACIÓN, no de negocio
        if (m.Cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(m), "La cantidad debe ser mayor que cero.");
        if (m.Origen is null && m.Destino is null)
            throw new ArgumentException("Un movimiento necesita origen, destino o ambos.", nameof(m));
        if (m.Origen is not null && m.Origen == m.Destino)
            throw new ArgumentException("El origen y el destino no pueden ser iguales.", nameof(m));
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AplicarAsync debe ejecutarse dentro de una transacción.");

        // Cuánto cambia cada ubicación: +cantidad si es destino, -cantidad si es origen
        var deltaPercha = Delta(m, UbicacionesStock.Percha);
        var deltaBodega = Delta(m, UbicacionesStock.Bodega);

        var variante = db.ProductoVariantes.Where(v =>
            v.NegocioId == tenant.NegocioId && v.VarianteId == m.VarianteId);

        // Si sale de algún lado, ese lado debe tener suficiente
        if (m.Origen == UbicacionesStock.Percha)
            variante = variante.Where(v => v.StockPercha >= m.Cantidad);
        if (m.Origen == UbicacionesStock.Bodega)
            variante = variante.Where(v => v.StockBodega >= m.Cantidad);

        // UNA sola sentencia SQL: verifica y mueve a la vez
        var filas = await variante.ExecuteUpdateAsync(s => s
            .SetProperty(v => v.StockPercha, v => v.StockPercha + deltaPercha)
            .SetProperty(v => v.StockBodega, v => v.StockBodega + deltaBodega), ct);

        if (filas == 0)
        {
            return null;
        }

        var stock = await db.ProductoVariantes
            .AsNoTracking()
            .Where(v => v.NegocioId == tenant.NegocioId && v.VarianteId == m.VarianteId)
            .Select(v => new { v.StockPercha, v.StockBodega })
            .FirstAsync(ct);

        var registro = new MovimientosInventario
        {
            NegocioId = tenant.NegocioId,
            VarianteId = m.VarianteId,
            UsuarioId = usuario.UsuarioId,
            TipoMovimiento = m.Tipo,
            UbicacionOrigen = m.Origen,
            UbicacionDestino = m.Destino,
            Cantidad = m.Cantidad,
            StockPerchaResultante = stock.StockPercha,
            StockBodegaResultante = stock.StockBodega,
            NotaExplicativa = m.Nota,
            FechaMovimiento = tiempo.GetUtcNow().UtcDateTime,
            ClientGuid = m.ClientGuid
        };

        db.Set<MovimientosInventario>().Add(registro);
        return registro;
    }

    private static int Delta(MovimientoStock m, string ubicacion) =>
        (m.Destino == ubicacion ? m.Cantidad : 0) - (m.Origen == ubicacion ? m.Cantidad : 0);
}