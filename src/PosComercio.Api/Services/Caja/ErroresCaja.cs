using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Caja;

public static class ErroresCaja
{
    public static readonly Error SinCajaAbierta = new(
        "caja.sin_caja_abierta",
        "No hay una caja abierta. Abre la caja para continuar.",
        StatusCodes.Status409Conflict);

    public static readonly Error CajaYaAbierta = new(
        "caja.ya_abierta",
        "Ya hay una caja abierta. Ciérrala antes de abrir otra.",
        StatusCodes.Status409Conflict);

    public static readonly Error CajaYaCerrada = new(
        "caja.ya_cerrada",
        "Esta caja ya fue cerrada por otra persona.",
        StatusCodes.Status409Conflict);

    public static readonly Error RetiroSoloDueno = new(
        "caja.retiro_solo_dueno",
        "Solo el dueño puede registrar retiros de efectivo.",
        StatusCodes.Status403Forbidden);

    public static readonly Error EfectivoInsuficiente = new(
        "caja.efectivo_insuficiente",
        "No hay suficiente efectivo en caja para ese monto.",
        StatusCodes.Status422UnprocessableEntity);
}