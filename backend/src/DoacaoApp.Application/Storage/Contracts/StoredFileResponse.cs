using DoacaoApp.Domain.Storage;

namespace DoacaoApp.Application.Storage.Contracts;

/// <summary>Representação de um arquivo armazenado para a API.</summary>
public sealed record StoredFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    StoredFileStatus Status,
    FileVisibility Visibility,
    int? Width,
    int? Height,
    string? OwnerType,
    Guid? OwnerId,
    DateTime CreatedAtUtc,
    string? Url);

public static class StoredFileMappings
{
    public static StoredFileResponse ToResponse(this StoredFile file, string? url = null) =>
        new(
            file.Id.Value,
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.Status,
            file.Visibility,
            file.Width,
            file.Height,
            file.OwnerType,
            file.OwnerId,
            file.CreatedAtUtc,
            url);
}
