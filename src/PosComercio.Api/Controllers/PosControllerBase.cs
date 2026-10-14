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
}