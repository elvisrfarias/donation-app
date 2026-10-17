using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using ErrorOr;

namespace DoacaoApp.Application.Sponsors.Commands;

/// <summary>Garante que o arquivo enviado como logo existe e é uma imagem.</summary>
internal static class SponsorLogo
{
    public static async Task<ErrorOr<StoredFile>> FindImageAsync(
        IStoredFileRepository fileRepository,
        Guid logoFileId,
        CancellationToken cancellationToken)
    {
        var logo = await fileRepository.GetByIdAsync(StoredFileId.From(logoFileId), cancellationToken);
        if (logo is null || logo.Status == StoredFileStatus.Deleted)
        {
            return DomainErrors.StoredFile.NotFound;
        }

        if (!logo.IsImage)
        {
            return DomainErrors.StoredFile.UnsupportedContentType(logo.ContentType);
        }

        return logo;
    }
}
