using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Inventario;

// ---------- Peticiones ----------

public sealed record TraspasoRequest(
    [Range(1, int.MaxValue)] int VarianteId,
    [Range(1, 1_000)] int Cantidad,
    [Required, RegularExpression("^(A_PERCHA|A_BODEGA)$", ErrorMessage = "Direccion debe ser A_PERCHA o A_BODEGA.")]
    string Direccion,
    [StringLength(200)] string? Nota,
    Guid? ClientGuid);

public sealed record ItemCompraRequest(
    [Range(1, int.MaxValue)] int VarianteId,
    [Range(1, 10_000)] int Cantidad,
    [Required, RegularExpression("^(PERCHA|BODEGA)$", ErrorMessage = "Destino debe ser PERCHA o BODEGA.")]
    string Destino,
    [Range(0, 100_000)] decimal? CostoUnitario);   // si viene, actualiza el costo de la variante

public sealed record CompraRequest(
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<ItemCompraRequest> Items,
    [StringLength(150)] string? Proveedor);

public sealed record AjusteRequest(
    [Range(1, int.MaxValue)] int VarianteId,
    [Required, RegularExpression("^(PERDIDA|DANADO|ENCONTRADO)$", ErrorMessage = "Tipo debe ser PERDIDA, DANADO o ENCONTRADO.")]
    string Tipo,
    [Required, RegularExpression("^(PERCHA|BODEGA)$", ErrorMessage = "Ubicacion debe ser PERCHA o BODEGA.")]
    string Ubicacion,
    [Range(1, 1_000)] int Cantidad,
    [Required, StringLength(200, MinimumLength = 5, ErrorMessage = "Explica qué pasó (5 a 200 caracteres).")]
    string Nota,
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos.")] string? PinAutorizacion);

public sealed record ConteoRequest(
    [Range(1, int.MaxValue)] int VarianteId,
    [Range(0, 100_000)] int ConteoPercha,
    [Range(0, 100_000)] int ConteoBodega,
    [StringLength(200)] string? Nota);

// ---------- Respuestas ----------

public sealed record MovimientoRealizadoDto(
    long MovimientoId,
    int VarianteId,
    string Producto,
    string Tipo,
    int Cantidad,
    int StockPercha,
    int StockBodega);

public sealed record CompraRealizadaDto(
    int ItemsRegistrados,
    int UnidadesIngresadas,
    IReadOnlyList<MovimientoRealizadoDto> Movimientos);

public sealed record ConteoResultadoDto(
    int VarianteId,
    string Producto,
    int AnteriorPercha,
    int AnteriorBodega,
    int ContadoPercha,
    int ContadoBodega,
    int DiferenciaPercha,
    int DiferenciaBodega,
    decimal ValorDiferencia);   // diferencia × costo: cuánto dinero representa

public sealed record ReponerPerchaItemDto(
    int VarianteId,
    int ProductoId,
    string Producto,
    string? Atributos,
    int StockBodega,
    string? UbicacionBodega,
    string? ReferenciaBodega,
    string? ReferenciaPercha);

public sealed record StockBajoItemDto(
    int VarianteId,
    int ProductoId,
    string Producto,
    string? Atributos,
    int StockPercha,
    int StockBodega,
    int StockMinimoAlerta);

public sealed record KardexItemDto(
    long MovimientoId,
    DateTime FechaUtc,
    string Tipo,
    string? Origen,
    string? Destino,
    int Cantidad,
    int StockPercha,
    int StockBodega,
    string Usuario,
    string? Nota,
    long? VentaId);