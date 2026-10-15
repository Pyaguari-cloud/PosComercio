namespace PosComercio.Api.Tests.Integracion.Infraestructura;

/// <summary>
/// Agrupa las pruebas de integración para que compartan UNA sola ApiFactory
/// (la base se crea una vez) y se ejecuten de una en una.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "Api";
}