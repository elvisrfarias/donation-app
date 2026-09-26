using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Representação de uma retirada registrada.</summary>
public sealed record WithdrawalResponse(
    Guid Id,
    Guid PointId,
    Guid BeneficiaryId,
    IReadOnlyList<string> Categories,
    DateTime OccurredAtUtc,
    string? Notes);

public static class WithdrawalMappings
{
    public static WithdrawalResponse ToResponse(this Withdrawal withdrawal) =>
        new(
            withdrawal.Id.Value,
            withdrawal.PointId.Value,
            withdrawal.BeneficiaryId.Value,
            withdrawal.Categories.Select(category => category.ToString()).ToList(),
            withdrawal.OccurredAtUtc,
            withdrawal.Notes);
}