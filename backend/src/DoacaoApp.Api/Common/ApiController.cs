using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DoacaoApp.Api.Common;

/// <summary>
/// Base dos controllers: injeta o MediatR e converte <see cref="ErrorOr{T}"/> em respostas
/// HTTP padronizadas (ProblemDetails / ValidationProblemDetails).
/// </summary>
[ApiController]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    protected IActionResult Respond<T>(ErrorOr<T> result, Func<T, IActionResult> onSuccess) =>
        result.Match(onSuccess, Problem);

    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Problem();
        }

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
            {
                modelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(modelState);
        }

        var first = errors[0];
        return Problem(
            detail: first.Description,
            statusCode: first.Type.ToStatusCode(),
            title: first.Code);
    }
}