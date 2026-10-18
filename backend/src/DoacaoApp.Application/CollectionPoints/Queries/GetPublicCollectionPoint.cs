using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Reviews;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Um ponto de coleta ativo, com os mesmos dados públicos da listagem do mapa — base da página própria do
/// ponto. Pontos inativos respondem como inexistentes.
/// </summary>
public sealed record GetPublicCollectionPointQuery(Guid PointId) : IRequest<ErrorOr<CollectionPointResponse>>;

public sealed class GetPublicCollectionPointHandler(
    ICollectionPointRepository repository,
    IReviewRepository reviewRepository,
    IStoredFileRepository fileRepository,
    IFileStorage storage)
    : IRequestHandler<GetPublicCollectionPointQuery, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        GetPublicCollectionPointQuery query,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(query.PointId), cancellationToken);
        if (point is null || !point.IsActive)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var summary = (await reviewRepository.GetSummariesAsync([point.Id], cancellationToken)).FirstOrDefault();
        var beneficiariesServed = await repository.GetBeneficiariesServedAsync([point.Id], cancellationToken);

        return point.ToResponse(
            await ResolveImageUrlAsync(point, cancellationToken),
            summary?.AverageRating ?? 0,
            summary?.ReviewCount ?? 0,
            beneficiariesServed.GetValueOrDefault(point.Id));
    }

    private async Task<string?> ResolveImageUrlAsync(CollectionPoint point, CancellationToken cancellationToken)
    {
        if (point.ImageFileId is not { } imageId)
        {
            return null;
        }

        var image = await fileRepository.GetByIdAsync(imageId, cancellationToken);
        if (image is null || image.Status == StoredFileStatus.Deleted)
        {
            return null;
        }

        var url = await storage.GetPresignedDownloadUrlAsync(image.ObjectKey, image.OriginalFileName, cancellationToken);
        return url.ToString();
    }
}
