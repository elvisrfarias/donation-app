using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Middleware;

/// <summary>
/// Handler global de exceções (IExceptionHandler do .NET 8+). Loga o erro e devolve um
/// ProblemDetails padronizado, sem vazar detalhes internos ao cliente.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exceção não tratada no request {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "Erro interno do servidor",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Ocorreu um erro inesperado ao processar a requisição.",
            },
        });
    }
}