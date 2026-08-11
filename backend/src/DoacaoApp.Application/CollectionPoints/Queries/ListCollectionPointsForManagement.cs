using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Lista todos os pontos de coleta (ativos e inativos) para a grid administrativa.
/// Restrita a Admin — diferente de <see cref="ListPublicCollectionPointsQuery"/>, que é pública
/// e só traz pontos ativos.
/// </summary>
public sealed record ListCollectionPointsForManagementQuery
    : IRequest<ErrorOr<IReadOnlyList<CollectionPointResponse>>>;

public sealed class ListCollectionPointsForManagementHandler(ICollectionPointRepository repository)
    : IRequestHandler<ListCollectionPointsForManagementQuery, ErrorOr<IReadOnlyList<CollectionPointResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<CollectionPointResponse>>> Handle(
        ListCollectionPointsForManagementQuery query,
        CancellationToken cancellationToken)
    {
        var points = await repository.ListAllAsync(cancellationToken);

        IReadOnlyList<CollectionPointResponse> response = points.Select(p => p.ToResponse()).ToList();

        return ErrorOrFactory.From(response);
    }
}
