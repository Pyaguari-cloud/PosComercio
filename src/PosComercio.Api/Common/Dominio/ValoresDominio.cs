namespace PosComercio.Api.Common.Dominio;

/// <summary>Valores que coinciden EXACTAMENTE con los CHECK de la base de datos.</summary>
public static class EstadosTurno
{
    public const string Abierto = "ABIERTO";
    public const string Cerrado = "CERRADO";
}

public static class EstadosVenta
{
    public const string Completada = "COMPLETADA";
    public const string Anulada = "ANULADA";
}

public static class MetodosPago
{
    public const string Efectivo = "EFECTIVO";
    public const string Transferencia = "TRANSFERENCIA";
    public const string Tarjeta = "TARJETA";
}

public static class TiposMovimientoCaja
{
    public const string Gasto = "GASTO";
    public const string Retiro = "RETIRO";
    public const string IngresoExtra = "INGRESO_EXTRA";
}