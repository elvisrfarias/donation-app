using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Domain.Donations;

/// <summary>
/// Doação prometida por alguém da comunidade a um ponto de coleta — Aggregate Root. A pessoa
/// avisa o que pretende entregar; o gestor do ponto confirma o recebimento ou cancela.
/// Referencia o ponto por Id (outro agregado).
/// </summary>
public sealed class Donation : AggregateRoot<DonationId>
{
    public const int MaxDonorNameLength = 120;
    public const int MaxDescriptionLength = 500;

    private Donation()
    {
        DonorName = string.Empty;
        Description = string.Empty;
    }

    private Donation(
        DonationId id,
        CollectionPointId pointId,
        string donorName,
        PhoneNumber? donorPhone,
        string description,
        DateTime createdAtUtc) : base(id)
    {
        PointId = pointId;
        DonorName = donorName;
        DonorPhone = donorPhone;
        Description = description;
        Status = DonationStatus.Pledged;
        CreatedAtUtc = createdAtUtc;
    }

    public CollectionPointId PointId { get; private set; }
    public string DonorName { get; private set; }
    public PhoneNumber? DonorPhone { get; private set; }
    public string Description { get; private set; }
    public DonationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static ErrorOr<Donation> Pledge(
        CollectionPointId pointId,
        string? donorName,
        string? donorPhone,
        string? description,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(donorName))
        {
            return DomainErrors.Donation.DonorRequired;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return DomainErrors.Donation.DescriptionRequired;
        }

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > MaxDescriptionLength)
        {
            return DomainErrors.Donation.DescriptionTooLong(MaxDescriptionLength);
        }

        var phoneResult = ParseOptionalPhone(donorPhone);
        if (phoneResult.IsError)
        {
            return phoneResult.Errors;
        }

        return new Donation(
            DonationId.New(), pointId, donorName.Trim(), phoneResult.Value, trimmedDescription, createdAtUtc);
    }

    public ErrorOr<Success> MarkReceived(DateTime receivedAtUtc) => Close(DonationStatus.Received, receivedAtUtc);

    public ErrorOr<Success> Cancel(DateTime cancelledAtUtc) => Close(DonationStatus.Cancelled, cancelledAtUtc);

    private ErrorOr<Success> Close(DonationStatus finalStatus, DateTime closedAtUtc)
    {
        if (Status != DonationStatus.Pledged)
        {
            return DomainErrors.Donation.AlreadyClosed;
        }

        Status = finalStatus;
        ClosedAtUtc = closedAtUtc;
        return Result.Success;
    }

    private static ErrorOr<PhoneNumber?> ParseOptionalPhone(string? donorPhone)
    {
        if (string.IsNullOrWhiteSpace(donorPhone))
        {
            return (PhoneNumber?)null;
        }

        var phone = PhoneNumber.Create(donorPhone);
        return phone.IsError ? phone.Errors : phone.Value;
    }
}
