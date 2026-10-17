using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;

namespace DoacaoApp.Application.Sponsors.Contracts;

/// <summary>Patrocinador com a URL (temporária) da logo. <c>Tier</c>: Diamond, Gold ou Silver.</summary>
public sealed record SponsorResponse(
    Guid Id,
    string Name,
    string Tier,
    string? WebsiteUrl,
    string? LogoUrl,
    bool IsActive);

public static class SponsorMappings
{
    public static SponsorResponse ToResponse(this Sponsor sponsor, string? logoUrl) =>
        new(sponsor.Id.Value, sponsor.Name, sponsor.Tier.ToString(), sponsor.WebsiteUrl, logoUrl, sponsor.IsActive);

    /// <summary>Monta as respostas resolvendo as logos de uma vez só (sem N+1 no banco).</summary>
    public static async Task<IReadOnlyList<SponsorResponse>> ToResponsesAsync(
        this IReadOnlyList<Sponsor> sponsors,
        IStoredFileRepository fileRepository,
        IFileStorage storage,
        CancellationToken cancellationToken)
    {
        var logoIds = sponsors.Select(s => s.LogoFileId).Distinct().ToList();
        var logos = logoIds.Count == 0 ? [] : await fileRepository.ListByIdsAsync(logoIds, cancellationToken);

        var urlByLogoId = new Dictionary<StoredFileId, string>();
        foreach (var logo in logos)
        {
            var url = await storage.GetPresignedDownloadUrlAsync(logo.ObjectKey, logo.OriginalFileName, cancellationToken);
            urlByLogoId[logo.Id] = url.ToString();
        }

        return [.. sponsors.Select(s => s.ToResponse(urlByLogoId.GetValueOrDefault(s.LogoFileId)))];
    }
}
