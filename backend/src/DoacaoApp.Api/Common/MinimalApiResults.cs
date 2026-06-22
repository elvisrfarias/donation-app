using ErrorOr;

namespace DoacaoApp.Api.Common;

/// <summary>Converte <see cref="ErrorOr{T}"/> em <see cref="IResult"/> (ProblemDetails) nas Minimal APIs.</summary>
public static class MinimalApiResults
{
    public static IResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var failures = errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return Results.ValidationProblem(failures);
        }

        var first = errors[0];
        return Results.Problem(
            statusCode: first.Type.ToStatusCode(),
            title: first.Description,
            extensions: new Dictionary<string, object?> { ["errorCode"] = first.Code });
    }

    public static IResult Match<T>(this ErrorOr<T> result, Func<T, IResult> onValue) =>
        result.Match(onValue, Problem);
}