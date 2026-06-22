using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Storage.Contracts;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Storage.Queries;

/// <summary>Metadados de um arquivo + URL temporária de download.</summary>
public sealed record GetStoredFileQuery(Guid FileId) : IRequest<ErrorOr<StoredFileResponse>>;

public sealed class GetStoredFileHandler(
    IStoredFileRepository repository,
    IFileStorage storage)
    : IRequestHandler<GetStoredFileQuery, ErrorOr<StoredFileResponse>>
{
    public async Task<ErrorOr<StoredFileResponse>> Handle(GetStoredFileQuery query, CancellationToken cancellationToken)
    {
        var file = await repository.GetByIdAsync(StoredFileId.From(query.FileId), cancellationToken);
        if (file is null || file.Status == StoredFileStatus.Deleted)
        {
            return DomainErrors.StoredFile.NotFound;
        }

        var url = await storage.GetPresignedDownloadUrlAsync(file.ObjectKey, file.OriginalFileName, cancellationToken);
        return file.ToResponse(url.ToString());
    }
}
