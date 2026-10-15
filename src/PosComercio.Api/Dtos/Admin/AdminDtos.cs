using System.ComponentModel.DataAnnotations;

namespace PosComercio.Api.Dtos.Admin;

public sealed record DuenoInicialRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, StringLength(80, MinimumLength = 3)] string UsuarioLogin,
    [Required, StringLength(100)] string Password);

public sealed record CrearNegocioRequest(
    [Required, StringLength(150, MinimumLength = 2)] string NombreComercial,
    [Required, StringLength(80, MinimumLength = 2)] string TipoNegocio,
    [StringLength(30)] string? TelefonoContacto,
    [Required] DuenoInicialRequest Dueno,
    [Range(0, 365)] int DiasPrueba = 30);

public sealed record CambiarPlanRequest(
    [Required, RegularExpression("^(PRUEBA|BASICO|PRO|SUSPENDIDO)$",
        ErrorMessage = "El plan debe ser PRUEBA, BASICO, PRO o SUSPENDIDO.")]
    string Plan);

public sealed record NegocioAdminDto(
    int NegocioId,
    string NombreComercial,
    string TipoNegocio,
    string? TelefonoContacto,
    string PlanActual,
    DateOnly? FechaFinPrueba,
    bool Activo,
    int CantidadUsuarios,
    DateTime FechaCreacionUtc);