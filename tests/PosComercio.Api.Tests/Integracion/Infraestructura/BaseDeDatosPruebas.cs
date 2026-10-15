using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PosComercio.Api.Tests.Integracion.Infraestructura;

/// <summary>
/// Recrea la base de pruebas desde cero usando los MISMOS scripts del repositorio.
/// </summary>
public static partial class BaseDeDatosPruebas
{
    private static readonly string[] Scripts = ["01_esquema_mvp.sql", "04_seguridad_rls.sql"];

    // Lotes que no aplican a la base de pruebas: la creación de la base real y los usuarios del servidor
    private static readonly string[] LotesOmitidos = ["CREATE DATABASE", "CREATE LOGIN", "CREATE USER", "ADD MEMBER"];

    public static async Task RecrearAsync()
    {
        SqlConnection.ClearAllPools();
        var nombre = ConfiguracionPruebas.NombreBaseDatos;

        await using (var master = new SqlConnection(ConfiguracionPruebas.CadenaMaster))
        {
            await master.OpenAsync();
            await EjecutarAsync(master, $"""
                IF DB_ID(N'{nombre}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{nombre}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{nombre}];
                END;
                CREATE DATABASE [{nombre}];
                """);
        }

        await using var conexion = new SqlConnection(ConfiguracionPruebas.CadenaConexion);
        await conexion.OpenAsync();

        foreach (var script in Scripts)
        {
            foreach (var lote in LeerLotes(script))
            {
                await EjecutarAsync(conexion, lote);
            }
        }
    }

    /// <summary>
    /// Divide un script por sus "GO" (que es una instrucción de SSMS, no de SQL Server)
    /// y quita los "USE" para que todo se ejecute en la base de pruebas.
    /// </summary>
    private static IEnumerable<string> LeerLotes(string archivo)
    {
        var contenido = File.ReadAllText(Path.Combine(CarpetaDatabase(), archivo));

        foreach (var lote in SeparadorGo().Split(contenido))
        {
            var sinUse = string.Join('\n', lote
                .Split('\n')
                .Where(linea => !linea.TrimStart().StartsWith("USE ", StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrWhiteSpace(sinUse) ||
                LotesOmitidos.Any(palabra => sinUse.Contains(palabra, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            yield return sinUse;
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();

    /// <summary>Busca la carpeta "database" subiendo desde la carpeta donde corren las pruebas.</summary>
    private static string CarpetaDatabase()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);

        while (carpeta is not null && !Directory.Exists(Path.Combine(carpeta.FullName, "database")))
        {
            carpeta = carpeta.Parent;
        }

        return carpeta is null
            ? throw new DirectoryNotFoundException("No se encontró la carpeta 'database' del repositorio.")
            : Path.Combine(carpeta.FullName, "database");
    }

    private static async Task EjecutarAsync(SqlConnection conexion, string sql)
    {
        await using var comando = new SqlCommand(sql, conexion) { CommandTimeout = 120 };
        await comando.ExecuteNonQueryAsync();
    }
}