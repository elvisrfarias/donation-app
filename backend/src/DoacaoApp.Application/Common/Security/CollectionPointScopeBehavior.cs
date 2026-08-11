using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Common.Security;

/// <summary>
/// Pipeline do MediatR que bloqueia Commands marcados com <see cref="IScopedToCollectionPoint"/>
/// quando o ponto informado não é o ponto do usuário autenticado. Admins não são restringidos.
/// </summary>
public sealed class CollectionPointScopeBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IScopedToCollectionPoint scoped || currentUser.IsAdmin)
        {
            return await next();
        }

        if (currentUser.CollectionPointId != scoped.CollectionPointId)
        {
            return (dynamic)new List<Error> { ScopeErrors.CollectionPointForbidden };
        }

        return await next();
    }
}
