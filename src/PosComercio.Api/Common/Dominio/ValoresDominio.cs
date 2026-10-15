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

public static class UbicacionesStock
{
    public const string Percha = "PERCHA";
    public const string Bodega = "BODEGA";
}

public static class TiposMovimientoInventario
{
    public const string Inicial = "INICIAL";
    public const string Compra = "COMPRA";
    public const string Venta = "VENTA";
    public const string PasarAPercha = "PASAR_A_PERCHA";
    public const string DevolverABodega = "DEVOLVER_A_BODEGA";
    public const string Perdida = "PERDIDA";
    public const string Encontrado = "ENCONTRADO";
    public const string Danado = "DANADO";
    public const string AjusteConteo = "AJUSTE_CONTEO";
    public const string Anulacion = "ANULACION";
}

public static class DireccionesTraspaso
{
    public const string APercha = "A_PERCHA";
    public const string ABodega = "A_BODEGA";
}