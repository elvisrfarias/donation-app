using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Storage.Contracts;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Storage.Queries;

/// <summary>Lista os arquivos de um dono (ex.: todas as fotos de um ponto de coleta).</summary>
public sealed record ListFilesByOwnerQuery(string OwnerType, Guid OwnerId)
    : IRequest<ErrorOr<IReadOnlyList<StoredFileResponse>>>;

public sealed class ListFilesByOwnerHandler(
    IStoredFileRepository repository,
    IFileStorage storage)
    : IRequestHandler<ListFilesByOwnerQuery, ErrorOr<IReadOnlyList<StoredFileResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<StoredFileResponse>>> Handle(
        ListFilesByOwnerQuery query,
        CancellationToken cancellationToken)
    {
        var files = await repository.ListByOwnerAsync(query.OwnerType, query.OwnerId, cancellationToken);

        var responses = new List<StoredFileResponse>(files.Count);
        foreach (var file in files)
        {
            // Presign é cálculo local (não chama o storage), então iterar é barato.
            var url = await storage.GetPresignedDownloadUrlAsync(file.ObjectKey, file.OriginalFileName, cancellationToken);
            responses.Add(file.ToResponse(url.ToString()));
        }

        return responses;
    }
}
