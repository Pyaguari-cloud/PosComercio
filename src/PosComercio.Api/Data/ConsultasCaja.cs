using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Dominio;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Data;

public static class ConsultasCaja
{
    /// <summary>
    /// Obtiene el turno abierto del negocio BLOQUEANDO su fila hasta que termine la transacción.
    /// Debe llamarse dentro de una transacción.
    /// Ventas y cierre de caja lo usan, así nunca ocurren al mismo tiempo.
    /// </summary>
    public static Task<CajaTurno?> BloquearTurnoAbiertoAsync(
        this PosComercioDbContext db, int negocioId, CancellationToken ct) =>
        db.CajaTurnos
          .FromSql($"""
              SELECT * FROM dbo.CajaTurnos WITH (UPDLOCK, ROWLOCK)
              WHERE NegocioId = {negocioId} AND Estado = {EstadosTurno.Abierto}
              """)
          .AsNoTracking()
          .FirstOrDefaultAsync(ct);
}