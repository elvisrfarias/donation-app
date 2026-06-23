using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Queries;

/// <summary>
/// Lista pública de pontos de coleta ativos (com endereço e contato).
/// Não requer autenticação — é a parte pública do sistema.
/// </summary>
public sealed record ListPublicCollectionPointsQuery
    : IRequest<ErrorOr<IReadOnlyList<CollectionPointResponse>>>;

public sealed class ListPublicCollectionPointsHandler(
    ICollectionPointRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage)
    : IRequestHandler<ListPublicCollectionPointsQuery, ErrorOr<IReadOnlyList<CollectionPointResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<CollectionPointResponse>>> Handle(
        ListPublicCollectionPointsQuery query,
        CancellationToken cancellationToken)
    {
        var points = await repository.ListActiveAsync(cancellationToken);

        // Resolve a URL (pré-assinada) de cada imagem em uma única consulta para evitar N+1.
        var imageIds = points
            .Where(p => p.ImageFileId is not null)
            .Select(p => p.ImageFileId!.Value)
            .Distinct()
            .ToList();

        var urlByFileId = new Dictionary<StoredFileId, string>();
        if (imageIds.Count > 0)
        {
            var files = await fileRepository.ListByIdsAsync(imageIds, cancellationToken);
            foreach (var file in files)
            {
                // Presign é cálculo local (não chama o storage), então iterar é barato.
                var url = await storage.GetPresignedDownloadUrlAsync(
                    file.ObjectKey, file.OriginalFileName, cancellationToken);
                urlByFileId[file.Id] = url.ToString();
            }
        }

        IReadOnlyList<CollectionPointResponse> response = points
            .Select(p => p.ToResponse(
                p.ImageFileId is { } id && urlByFileId.TryGetValue(id, out var url) ? url : null))
            .ToList();

        return ErrorOrFactory.From(response);
    }
}
