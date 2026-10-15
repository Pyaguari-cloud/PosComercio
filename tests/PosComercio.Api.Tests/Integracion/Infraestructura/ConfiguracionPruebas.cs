namespace PosComercio.Api.Tests.Integracion.Infraestructura;

public static class ConfiguracionPruebas
{
    // ⚠️ Pon aquí el mismo servidor que usas en SSMS (por ejemplo localhost\SQLEXPRESS)
    private const string ServidorPorDefecto = @"localhost\SQLEXPRESS";

    public const string NombreBaseDatos = "PosComercio_Pruebas";

    /// <summary>
    /// Se puede cambiar sin tocar el código con la variable de entorno POS_PRUEBAS_SERVIDOR
    /// (útil en otra computadora o en un servidor de integración continua).
    /// </summary>
    public static string Servidor =>
        Environment.GetEnvironmentVariable("POS_PRUEBAS_SERVIDOR") ?? ServidorPorDefecto;

    public static string CadenaConexion =>
        $"Server={Servidor};Database={NombreBaseDatos};Trusted_Connection=True;TrustServerCertificate=True";

    public static string CadenaMaster =>
        $"Server={Servidor};Database=master;Trusted_Connection=True;TrustServerCertificate=True";
}