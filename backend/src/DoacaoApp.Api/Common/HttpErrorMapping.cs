using ErrorOr;

namespace DoacaoApp.Api.Common;

/// <summary>Traduz o <see cref="ErrorType"/> do Result Pattern para status HTTP.</summary>
public static class HttpErrorMapping
{
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Status representativo de uma lista de erros (usa o primeiro).</summary>
    public static int PrimaryStatusCode(this List<Error> errors) =>
        errors.Count == 0 ? StatusCodes.Status500InternalServerError : errors[0].Type.ToStatusCode();
}