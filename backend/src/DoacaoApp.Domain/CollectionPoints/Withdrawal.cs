using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Retirada — Entity DENTRO do agregado <see cref="CollectionPoint"/>. Referencia o
/// beneficiário por Id (é outro agregado). O conjunto de retiradas de um beneficiário,
/// somado entre todos os pontos, forma o histórico municipal compartilhado.
/// </summary>
public sealed class Withdrawal : Entity<WithdrawalId>
{
    private Withdrawal()
    {
    }

    private Withdrawal(
        WithdrawalId id,
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        DateTime occurredAtUtc,
        string? notes) : base(id)
    {
        PointId = pointId;
        BeneficiaryId = beneficiaryId;
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
    }

    public CollectionPointId PointId { get; private set; }
    public BeneficiaryId BeneficiaryId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }

    internal static Withdrawal Create(
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        DateTime occurredAtUtc,
        string? notes) =>
        new(WithdrawalId.New(), pointId, beneficiaryId, occurredAtUtc,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
}
