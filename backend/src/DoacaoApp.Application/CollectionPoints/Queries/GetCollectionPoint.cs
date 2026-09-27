using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Consulta um ponto de coleta (ativo ou inativo), incluindo o estoque. Um gestor de ponto só
/// consegue consultar o próprio ponto — o <see cref="CollectionPointScopeBehavior{TRequest, TResponse}"/>
/// bloqueia o acesso a qualquer outro. Admins consultam qualquer ponto.
/// </summary>
public sealed record GetCollectionPointQuery(Guid PointId)
    : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class GetCollectionPointHandler(ICollectionPointRepository repository)
    : IRequestHandler<GetCollectionPointQuery, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        GetCollectionPointQuery query,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(query.PointId), cancellationToken);

        return point is null ? DomainErrors.CollectionPoint.NotFound : point.ToResponse(includeManagementDetails: true);
    }
}
