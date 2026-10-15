using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Ventas;

// ---------- Petición ----------

public sealed record ItemVentaRequest(
    [Range(1, int.MaxValue)] int VarianteId,
    [Range(1, 1000)] int Cantidad,
    [Required, RegularExpression("^(PERCHA|BODEGA)$", ErrorMessage = "SalioDe debe ser PERCHA o BODEGA.")]
    string SalioDe,
    [Range(0, 100_000)] decimal PrecioVendidoUnitario);

public sealed record PagoVentaRequest(
    [Required, RegularExpression("^(EFECTIVO|TRANSFERENCIA|TARJETA)$",
        ErrorMessage = "MetodoPago debe ser EFECTIVO, TRANSFERENCIA o TARJETA.")]
    string MetodoPago,
    [Range(0.01, 100_000)] decimal Monto,
    [StringLength(100)] string? Referencia);

public sealed record RegistrarVentaRequest(
    [Required, MinLength(1), MaxLength(50)] IReadOnlyList<ItemVentaRequest> Items,
    [Required, MinLength(1), MaxLength(5)] IReadOnlyList<PagoVentaRequest> Pagos,
    [StringLength(250)] string? Nota,
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos.")] string? PinAutorizacion,
    Guid? ClientGuid);

// ---------- Respuesta ----------

public sealed record VentaDetalleDto(
    long VentaDetalleId,
    int VarianteId,
    string Producto,
    string SalioDe,
    int Cantidad,
    decimal PrecioLista,
    decimal PrecioVendido,
    decimal DescuentoUnitario,
    decimal Subtotal,
    string? AutorizadoPor);

public sealed record VentaPagoDto(
    string MetodoPago,
    decimal Monto,
    string? Referencia);

public sealed record VentaDto(
    long VentaId,
    int NumeroVenta,
    DateTime FechaVentaUtc,
    string Vendedor,
    string Estado,
    decimal TotalVenta,
    string? Nota,
    IReadOnlyList<VentaDetalleDto> Detalles,
    IReadOnlyList<VentaPagoDto> Pagos,
    decimal TotalRebajado,
    decimal? Ganancia);   // solo el dueño