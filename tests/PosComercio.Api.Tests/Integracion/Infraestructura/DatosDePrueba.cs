using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Tests.Integracion.Infraestructura;

/// <summary>Lo que cada prueba recibe: un negocio nuevo, listo para usar.</summary>
public sealed record Escenario(
    int NegocioId,
    string DuenoLogin,
    string VendedorLogin,
    int ProductoId,
    int VarianteConStockId,       // Talla 40: 5 en percha, 5 en bodega · costo 20, lista 50, mínimo 40
    int VarianteUltimaUnidadId);  // Talla 41: 1 en percha, 0 en bodega · costo 10, lista 30, mínimo 25

public static class DatosDePrueba
{
    public const string Password = "Prueba2026";
    public const string PinDueno = "1357";

    private const string ConfiguracionNegocio = """{"cierreCiego":true,"montoInicialSugerido":20}""";

    /// <summary>Crea un negocio NUEVO con dueño, vendedor y un producto con dos tallas.</summary>
    public static async Task<Escenario> CrearNegocioAsync()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var hasher = new PasswordHasher<Usuario>();

        await using var conexion = new SqlConnection(ConfiguracionPruebas.CadenaConexion);
        await conexion.OpenAsync();

        var negocioId = await EscalarAsync<int>(conexion,
            "INSERT INTO dbo.Negocios (NombreComercial, TipoNegocio, ConfiguracionJson) " +
            "OUTPUT INSERTED.NegocioId VALUES (@nombre, N'Zapateria', @config);",
            ("@nombre", $"Negocio {sufijo}"), ("@config", ConfiguracionNegocio));

        // Igual que hace la API: a partir de aquí, esta conexión "pertenece" al negocio nuevo (RLS)
        await EjecutarAsync(conexion,
            "EXEC sp_set_session_context @key = N'NegocioId', @value = @id;", ("@id", negocioId));

        var duenoLogin = $"dueno-{sufijo}@pruebas.com";
        var vendedorLogin = $"vendedor-{sufijo}@pruebas.com";

        const string insertarUsuario =
            "INSERT INTO dbo.Usuarios (NegocioId, Nombre, UsuarioLogin, PasswordHash, PinHash, Rol) " +
            "VALUES (@negocio, @nombre, @login, @hash, @pin, @rol);";

        await EjecutarAsync(conexion, insertarUsuario,
            ("@negocio", negocioId), ("@nombre", "Dueño"), ("@login", duenoLogin),
            ("@hash", hasher.HashPassword(new Usuario(), Password)),
            ("@pin", hasher.HashPassword(new Usuario(), PinDueno)), ("@rol", "Dueno"));

        await EjecutarAsync(conexion, insertarUsuario,
            ("@negocio", negocioId), ("@nombre", "Vendedor"), ("@login", vendedorLogin),
            ("@hash", hasher.HashPassword(new Usuario(), Password)),
            ("@pin", null), ("@rol", "Vendedor"));

        var productoId = await EscalarAsync<int>(conexion,
            "INSERT INTO dbo.Productos (NegocioId, Nombre, TieneVariantes) " +
            "OUTPUT INSERTED.ProductoId VALUES (@negocio, N'Zapato de Prueba', 1);",
            ("@negocio", negocioId));

        const string insertarVariante = """
            INSERT INTO dbo.ProductoVariantes
                (NegocioId, ProductoId, EsVarianteUnica, AtributosResumen, AtributosJson,
                 PrecioCosto, PrecioVentaLista, PrecioMinimoRegateo, StockPercha, StockBodega)
            OUTPUT INSERTED.VarianteId
            VALUES (@negocio, @producto, 0, @resumen, @json, @costo, @lista, @minimo, @percha, @bodega);
            """;

        var conStock = await EscalarAsync<int>(conexion, insertarVariante,
            ("@negocio", negocioId), ("@producto", productoId),
            ("@resumen", "Talla 40"), ("@json", """{"talla":"40"}"""),
            ("@costo", 20m), ("@lista", 50m), ("@minimo", 40m), ("@percha", 5), ("@bodega", 5));

        var ultimaUnidad = await EscalarAsync<int>(conexion, insertarVariante,
            ("@negocio", negocioId), ("@producto", productoId),
            ("@resumen", "Talla 41"), ("@json", """{"talla":"41"}"""),
            ("@costo", 10m), ("@lista", 30m), ("@minimo", 25m), ("@percha", 1), ("@bodega", 0));

        return new Escenario(negocioId, duenoLogin, vendedorLogin, productoId, conStock, ultimaUnidad);
    }

    private static async Task<T> EscalarAsync<T>(SqlConnection conexion, string sql, params (string Nombre, object? Valor)[] parametros)
    {
        await using var comando = CrearComando(conexion, sql, parametros);
        return (T)(await comando.ExecuteScalarAsync())!;
    }

    private static async Task EjecutarAsync(SqlConnection conexion, string sql, params (string Nombre, object? Valor)[] parametros)
    {
        await using var comando = CrearComando(conexion, sql, parametros);
        await comando.ExecuteNonQueryAsync();
    }

    private static SqlCommand CrearComando(SqlConnection conexion, string sql, (string Nombre, object? Valor)[] parametros)
    {
        var comando = new SqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }
        return comando;
    }
}