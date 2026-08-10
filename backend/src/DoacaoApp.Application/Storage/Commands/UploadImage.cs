using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Storage.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Storage.Commands;

/// <summary>Envia uma imagem para o storage e registra seus metadados.</summary>
public sealed record UploadImageCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    FileVisibility Visibility,
    string? OwnerType,
    Guid? OwnerId) : IRequest<ErrorOr<StoredFileResponse>>;

public sealed class UploadImageValidator : AbstractValidator<UploadImageCommand>
{
    public UploadImageValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(SupportedImage.IsAllowed)
            .WithMessage("Tipo de arquivo não suportado. Use JPEG, PNG, WebP, GIF ou AVIF.");
        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("O arquivo está vazio.")
            .LessThanOrEqualTo(SupportedImage.MaxFileSizeBytes)
            .WithMessage($"O arquivo excede o limite de {SupportedImage.MaxFileSizeMegabytes} MB.");

        RuleFor(x => x.Content)
            .MustAsync((command, _, ct) => SupportedImage.MatchesFileSignatureAsync(command.Content, command.ContentType, ct))
            .WithMessage("O conteúdo do arquivo não corresponde ao tipo declarado.")
            .WithName(nameof(UploadImageCommand.Content))
            .When(x => SupportedImage.IsAllowed(x.ContentType), ApplyConditionTo.CurrentValidator);
    }
}

public sealed class UploadImageHandler(
    IFileStorage storage,
    IStoredFileRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    ILogger<UploadImageHandler> logger)
    : IRequestHandler<UploadImageCommand, ErrorOr<StoredFileResponse>>
{
    public async Task<ErrorOr<StoredFileResponse>> Handle(
        UploadImageCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var objectKey = BuildObjectKey(command.Visibility, command.FileName, now);

        StoredObject stored;
        try
        {
            stored = await storage.UploadAsync(
                new FileUploadRequest(
                    objectKey,
                    command.Content,
                    command.ContentType,
                    command.Length,
                    command.Visibility,
                    command.FileName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no upload do arquivo {FileName} para a chave {Key}.", command.FileName, objectKey);
            return DomainErrors.StoredFile.UploadFailed;
        }

        var fileResult = StoredFile.Create(
            stored.Provider,
            stored.Bucket,
            stored.ObjectKey,
            command.FileName,
            command.ContentType,
            stored.SizeBytes > 0 ? stored.SizeBytes : command.Length,
            command.Visibility,
            now,
            uploadedByUserId: currentUser.UserId,
            eTag: stored.ETag,
            ownerType: command.OwnerType,
            ownerId: command.OwnerId);

        if (fileResult.IsError)
        {
            await TryCleanupAsync(stored.ObjectKey, cancellationToken);
            return fileResult.Errors;
        }

        repository.Add(fileResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var url = await storage.GetPresignedDownloadUrlAsync(stored.ObjectKey, command.FileName, cancellationToken);
        return fileResult.Value.ToResponse(url.ToString());
    }

    private static string BuildObjectKey(FileVisibility visibility, string fileName, DateTime nowUtc)
    {
        var prefix = visibility == FileVisibility.Public ? "public" : "private";
        var extension = (Path.GetExtension(fileName) ?? string.Empty).Trim().ToLowerInvariant();
        return $"{prefix}/images/{nowUtc:yyyy}/{nowUtc:MM}/{Guid.NewGuid():N}{extension}";
    }

    private async Task TryCleanupAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await storage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao limpar objeto órfão {Key} após erro de registro.", objectKey);
        }
    }
}
