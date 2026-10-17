using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using ErrorOr;

namespace DoacaoApp.Domain.Sponsors;

/// <summary>
/// Instituição (igreja, empresa, associação...) que patrocina mensalmente a manutenção do projeto — Aggregate
/// Root. Aparece na página pública com a logo, agrupada pela <see cref="SponsorTier"/>, enquanto estiver ativa.
/// </summary>
public sealed class Sponsor : AggregateRoot<SponsorId>
{
    public const int MaxNameLength = 120;
    public const int MaxWebsiteUrlLength = 300;

    private Sponsor()
    {
        Name = string.Empty;
    }

    private Sponsor(
        SponsorId id,
        string name,
        SponsorTier tier,
        string? websiteUrl,
        StoredFileId logoFileId,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Tier = tier;
        WebsiteUrl = websiteUrl;
        LogoFileId = logoFileId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; }
    public SponsorTier Tier { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public StoredFileId LogoFileId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ErrorOr<Sponsor> Create(
        string? name,
        SponsorTier tier,
        string? websiteUrl,
        StoredFileId logoFileId,
        DateTime createdAtUtc)
    {
        var details = ValidateDetails(name, tier, websiteUrl);
        if (details.IsError)
        {
            return details.Errors;
        }

        return new Sponsor(
            SponsorId.New(), details.Value.Name, tier, details.Value.WebsiteUrl, logoFileId, createdAtUtc);
    }

    public ErrorOr<Updated> UpdateDetails(string? name, SponsorTier tier, string? websiteUrl, bool isActive)
    {
        var details = ValidateDetails(name, tier, websiteUrl);
        if (details.IsError)
        {
            return details.Errors;
        }

        Name = details.Value.Name;
        Tier = tier;
        WebsiteUrl = details.Value.WebsiteUrl;
        IsActive = isActive;
        return Result.Updated;
    }

    public void ChangeLogo(StoredFileId logoFileId) => LogoFileId = logoFileId;

    private static ErrorOr<(string Name, string? WebsiteUrl)> ValidateDetails(
        string? name,
        SponsorTier tier,
        string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Sponsor.NameRequired;
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            return DomainErrors.Sponsor.NameTooLong(MaxNameLength);
        }

        if (!Enum.IsDefined(tier))
        {
            return DomainErrors.Sponsor.InvalidTier;
        }

        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            return (trimmedName, null);
        }

        var trimmedUrl = websiteUrl.Trim();
        if (trimmedUrl.Length > MaxWebsiteUrlLength || !IsHttpUrl(trimmedUrl))
        {
            return DomainErrors.Sponsor.InvalidWebsiteUrl;
        }

        return (trimmedName, trimmedUrl);
    }

    private static bool IsHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
