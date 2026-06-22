using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Common.Behaviors;

/// <summary>
/// Pipeline do MediatR que roda os validators (FluentValidation) ANTES do handler.
/// Em caso de falha, retorna os erros como <see cref="Error.Validation"/> via Result Pattern,
/// sem lançar exception — o handler nem chega a executar.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .Distinct()
            .ToList();

        if (errors.Count == 0)
        {
            return await next();
        }

        // ErrorOr<T> possui conversão implícita de List<Error>; usamos dynamic para
        // satisfazer o TResponse genérico em tempo de execução.
        return (dynamic)errors;
    }
}