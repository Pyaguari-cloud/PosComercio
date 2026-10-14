using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Caja;

// ---------- Peticiones ----------

public sealed record AbrirCajaRequest(
    [Range(0, 100_000)] decimal MontoInicial);

public sealed record RegistrarMovimientoCajaRequest(
    [Required, RegularExpression("^(GASTO|RETIRO|INGRESO_EXTRA)$",
        ErrorMessage = "El tipo debe ser GASTO, RETIRO o INGRESO_EXTRA.")]
    string Tipo,
    [Range(0.01, 100_000)] decimal Monto,
    [Required, StringLength(200, MinimumLength = 2)] string Concepto,
    Guid? ClientGuid);

public sealed record CerrarCajaRequest(
    [Range(0, 1_000_000)] decimal EfectivoContado,
    [StringLength(250)] string? Nota);

public sealed record PaginacionQuery
{
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;
}

// ---------- Respuestas ----------

public sealed record MovimientoCajaDto(
    long CajaMovimientoId,
    string Tipo,
    decimal Monto,
    string Concepto,
    DateTime FechaUtc,
    string RegistradoPor);

public sealed record TurnoActualDto(
    long TurnoId,
    DateTime FechaAperturaUtc,
    string AbiertoPor,
    decimal MontoInicial,
    int CantidadVentas,
    decimal TotalVentas,
    decimal VentasEfectivo,
    decimal VentasTransferencia,
    decimal VentasTarjeta,
    decimal GastosRetiros,
    decimal IngresosExtra,
    decimal? EfectivoEsperado,   // null si hay cierre ciego y no es el dueño
    decimal? Ganancia,           // null si no es el dueño
    IReadOnlyList<MovimientoCajaDto> Movimientos);

/// <summary>
/// Respuesta de GET /api/caja/actual. Con UNA sola llamada,
/// el frontend sabe si mostrar "Abrir caja" o el resumen del día.
/// </summary>
public sealed record EstadoCajaDto(
    bool HayCajaAbierta,
    bool CierreCiego,
    decimal MontoInicialSugerido,
    TurnoActualDto? Turno);

public sealed record CierreCajaDto(
    long TurnoId,
    DateTime FechaAperturaUtc,
    DateTime? FechaCierreUtc,
    string AbiertoPor,
    string? CerradoPor,
    decimal MontoInicial,
    decimal TotalVentas,
    decimal VentasEfectivo,
    decimal VentasTransferencia,
    decimal VentasTarjeta,
    decimal GastosRetiros,
    decimal IngresosExtra,
    decimal EfectivoEsperado,
    decimal EfectivoContado,
    decimal Diferencia,
    decimal? Ganancia,
    string? Nota);