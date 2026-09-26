using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Lista pública de pontos de coleta ativos (endereço, contato e resumo das avaliações).
/// Não requer autenticação — é a parte pública do sistema. Quando <see cref="TopRatedFirst"/>
/// é verdadeiro, ordena pelos pontos mais bem avaliados (ranking).
/// </summary>
public sealed record ListPublicCollectionPointsQuery(bool TopRatedFirst = false)
    : IRequest<ErrorOr<IReadOnlyList<CollectionPointResponse>>>;

public sealed class ListPublicCollectionPointsHandler(
    ICollectionPointRepository repository,
    IReviewRepository reviewRepository,
    IStoredFileRepository fileRepository,
    IFileStorage storage)
    : IRequestHandler<ListPublicCollectionPointsQuery, ErrorOr<IReadOnlyList<CollectionPointResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<CollectionPointResponse>>> Handle(
        ListPublicCollectionPointsQuery query,
        CancellationToken cancellationToken)
    {
        var points = await repository.ListActiveAsync(cancellationToken);

        var urlByFileId = await ResolveImageUrlsAsync(points, cancellationToken);

        var pointIds = points.Select(p => p.Id).ToList();

        var summaryByPoint = (await reviewRepository.GetSummariesAsync(pointIds, cancellationToken))
            .ToDictionary(s => s.PointId);

        var beneficiariesServedByPoint = await repository.GetBeneficiariesServedAsync(pointIds, cancellationToken);

        var responses = points.Select(p =>
        {
            var imageUrl = p.ImageFileId is { } id && urlByFileId.TryGetValue(id, out var url) ? url : null;
            var summary = summaryByPoint.GetValueOrDefault(p.Id);
            return p.ToResponse(
                imageUrl,
                summary?.AverageRating ?? 0,
                summary?.ReviewCount ?? 0,
                beneficiariesServedByPoint.GetValueOrDefault(p.Id));
        });

        if (query.TopRatedFirst)
        {
            responses = responses
                .OrderByDescending(r => r.AverageRating)
                .ThenByDescending(r => r.ReviewCount)
                .ThenBy(r => r.Name);
        }

        return ErrorOrFactory.From<IReadOnlyList<CollectionPointResponse>>([.. responses]);
    }

    private async Task<Dictionary<StoredFileId, string>> ResolveImageUrlsAsync(
        IReadOnlyList<CollectionPoint> points,
        CancellationToken cancellationToken)
    {
        var imageIds = points
            .Where(p => p.ImageFileId is not null)
            .Select(p => p.ImageFileId!.Value)
            .Distinct()
            .ToList();

        var urlByFileId = new Dictionary<StoredFileId, string>();
        if (imageIds.Count == 0)
        {
            return urlByFileId;
        }

        var files = await fileRepository.ListByIdsAsync(imageIds, cancellationToken);
        foreach (var file in files)
        {
            var url = await storage.GetPresignedDownloadUrlAsync(
                file.ObjectKey, file.OriginalFileName, cancellationToken);
            urlByFileId[file.Id] = url.ToString();
        }

        return urlByFileId;
    }
}
