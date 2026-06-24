using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Storage.Commands;

/// <summary>Remove um arquivo: apaga o objeto do bucket e marca o metadado como removido.</summary>
public sealed record DeleteFileCommand(Guid FileId) : IRequest<ErrorOr<Deleted>>;

public sealed class DeleteFileHandler(
    IFileStorage storage,
    IStoredFileRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<DeleteFileHandler> logger)
    : IRequestHandler<DeleteFileCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        var file = await repository.GetByIdAsync(StoredFileId.From(command.FileId), cancellationToken);
        if (file is null)
        {
            return DomainErrors.StoredFile.NotFound;
        }

        try
        {
            await storage.DeleteAsync(file.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao apagar objeto {Key} do storage; metadado será marcado como removido.", file.ObjectKey);
        }

        file.MarkDeleted(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
