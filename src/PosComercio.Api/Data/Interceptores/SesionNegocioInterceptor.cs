using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PosComercio.Api.Common.Seguridad;

namespace PosComercio.Api.Data.Interceptores;

/// <summary>
/// Cada vez que EF Core abre una conexión, le dice a SQL Server de qué negocio
/// es la petición (SESSION_CONTEXT). Las políticas RLS usan ese valor para filtrar filas.
/// </summary>
public sealed class SesionNegocioInterceptor(IHttpContextAccessor accessor) : DbConnectionInterceptor
{
    private const string Sql =
        "EXEC sp_set_session_context @key = N'NegocioId', @value = @negocioId; " +
        "EXEC sp_set_session_context @key = N'EsAdminSaaS', @value = @esAdmin;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var comando = CrearComando(connection);
        comando.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var comando = CrearComando(connection);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CrearComando(DbConnection connection)
    {
        var usuario = accessor.HttpContext?.User;

        // Sin usuario autenticado (login, health check, arranque) → NULL: el RLS no muestra nada
        int? negocioId = int.TryParse(usuario?.FindFirst(ClaimsPos.NegocioId)?.Value, out var id) ? id : null;
        var esAdmin = usuario?.FindFirst(ClaimsPos.Rol)?.Value == Roles.AdminSaaS;

        var comando = connection.CreateCommand();
        comando.CommandText = Sql;
        AgregarParametro(comando, "@negocioId", DbType.Int32, negocioId);
        AgregarParametro(comando, "@esAdmin", DbType.Boolean, esAdmin);
        return comando;
    }

    private static void AgregarParametro(DbCommand comando, string nombre, DbType tipo, object? valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.DbType = tipo;
        parametro.Value = valor ?? DBNull.Value;
        comando.Parameters.Add(parametro);
    }
}