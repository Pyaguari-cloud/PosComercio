using Microsoft.AspNetCore.Mvc;
using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Controllers;

/// <summary>
/// Base de todos los controllers: traduce errores de negocio a respuestas ProblemDetails.
/// </summary>
[ApiController]
public abstract class PosControllerBase : ControllerBase
{
    protected ObjectResult Problema(Error error)
    {
        var problema = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: error.StatusHttp,
            title: error.Mensaje);

        problema.Extensions["codigo"] = error.Codigo;

        return new ObjectResult(problema) { StatusCode = error.StatusHttp };
    }

    /// <summary>Convierte un Resultado en 200 OK o en el error correspondiente.</summary>
    protected ActionResult<T> Responder<T>(Resultado<T> resultado) =>
        resultado.Exito ? Ok(resultado.Valor) : Problema(resultado.Error);
}