using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Lista pública de pontos de coleta ativos (com endereço e contato).
/// Não requer autenticação — é a parte pública do sistema.
/// </summary>
public sealed record ListPublicCollectionPointsQuery
    : IRequest<ErrorOr<IReadOnlyList<CollectionPointResponse>>>;

public sealed class ListPublicCollectionPointsHandler(ICollectionPointRepository repository)
    : IRequestHandler<ListPublicCollectionPointsQuery, ErrorOr<IReadOnlyList<CollectionPointResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<CollectionPointResponse>>> Handle(
        ListPublicCollectionPointsQuery query,
        CancellationToken cancellationToken)
    {
        var points = await repository.ListActiveAsync(cancellationToken);

        IReadOnlyList<CollectionPointResponse> response =
            points.Select(p => p.ToResponse()).ToList();

        return ErrorOrFactory.From(response);
    }
}