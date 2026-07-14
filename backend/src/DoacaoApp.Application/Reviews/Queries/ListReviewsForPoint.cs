using DoacaoApp.Application.Reviews.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Reviews.Queries;

/// <summary>Lista as avaliações de um ponto de coleta (mais recentes primeiro). Público.</summary>
public sealed record ListReviewsForPointQuery(Guid CollectionPointId)
    : IRequest<ErrorOr<IReadOnlyList<ReviewResponse>>>;

public sealed class ListReviewsForPointHandler(IReviewRepository repository)
    : IRequestHandler<ListReviewsForPointQuery, ErrorOr<IReadOnlyList<ReviewResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<ReviewResponse>>> Handle(
        ListReviewsForPointQuery query,
        CancellationToken cancellationToken)
    {
        var reviews = await repository.ListByPointAsync(
            CollectionPointId.From(query.CollectionPointId), cancellationToken);

        IReadOnlyList<ReviewResponse> response = [.. reviews.Select(r => r.ToResponse())];

        return ErrorOrFactory.From(response);
    }
}
