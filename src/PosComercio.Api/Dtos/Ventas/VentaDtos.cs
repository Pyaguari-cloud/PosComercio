using System.ComponentModel.DataAnnotations;
using PosComercio.Api.Dtos.Comunes;
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
    decimal? Ganancia,          // solo el dueño
    string? MotivoAnulacion,    // NUEVO
    string? AnuladaPor);        // NUEVO);   

// ---------- Cuaderno del día ----------

public sealed record CuadernoQuery
{
    /// <summary>Fecha local del negocio. Si no se envía, es hoy.</summary>
    public DateOnly? Fecha { get; init; }

    /// <summary>Filtrar por vendedor (opcional).</summary>
    public int? UsuarioId { get; init; }

    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 50;
}

public sealed record VentaResumenDto(
    long VentaId,
    int NumeroVenta,
    DateTime FechaVentaUtc,
    string Vendedor,
    string Estado,
    decimal TotalVenta,
    int CantidadArticulos,
    IReadOnlyList<string> Productos,
    IReadOnlyList<string> MetodosPago);

public sealed record CuadernoDto(
    DateOnly Fecha,
    int CantidadVentas,
    int CantidadAnuladas,
    decimal TotalVendido,
    decimal TotalEfectivo,
    decimal TotalTransferencia,
    decimal TotalTarjeta,
    decimal TotalRebajado,
    decimal? Ganancia,                       // solo el dueño
    PaginaDto<VentaResumenDto> Ventas);

// ---------- Anulación ----------

public sealed record AnularVentaRequest(
    [Required, StringLength(200, MinimumLength = 5, ErrorMessage = "Explica el motivo (5 a 200 caracteres).")]
    string Motivo,
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos.")]
    string? PinAutorizacion);