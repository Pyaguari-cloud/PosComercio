namespace PosComercio.Api.Common.Resultados;

/// <summary>
/// Un error de negocio esperado.
/// Codigo: identificador estable para que el frontend reaccione (ej. "auth.credenciales_invalidas").
/// Mensaje: texto para mostrar al usuario.
/// </summary>
public sealed record Error(string Codigo, string Mensaje, int StatusHttp);