using DoacaoApp.Domain.Donations;

namespace DoacaoApp.Application.Donations.Contracts;

/// <summary>Representação de uma doação prometida a um ponto de coleta.</summary>
public sealed record DonationResponse(
    Guid Id,
    Guid CollectionPointId,
    string DonorName,
    string? DonorPhone,
    string Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc);

public static class DonationMappings
{
    public static DonationResponse ToResponse(this Donation donation) =>
        new(
            donation.Id.Value,
            donation.PointId.Value,
            donation.DonorName,
            donation.DonorPhone?.Formatted,
            donation.Description,
            donation.Status.ToString(),
            donation.CreatedAtUtc,
            donation.ClosedAtUtc);
}
