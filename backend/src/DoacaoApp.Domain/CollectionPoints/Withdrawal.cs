using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Retirada — Entity DENTRO do agregado <see cref="CollectionPoint"/>. Referencia
/// família e beneficiário por Id (são outros agregados). O conjunto de retiradas de
/// uma família, somado entre todos os pontos, forma o histórico municipal compartilhado.
/// </summary>
public sealed class Withdrawal : Entity<WithdrawalId>
{
    // Usado pela materialização do EF Core.
    private Withdrawal()
    {
    }

    private Withdrawal(
        WithdrawalId id,
        CollectionPointId pointId,
        FamilyId familyId,
        BeneficiaryId beneficiaryId,
        DateTime occurredAtUtc,
        string? notes) : base(id)
    {
        PointId = pointId;
        FamilyId = familyId;
        BeneficiaryId = beneficiaryId;
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
    }

    public CollectionPointId PointId { get; private set; }
    public FamilyId FamilyId { get; private set; }
    public BeneficiaryId BeneficiaryId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }

    internal static Withdrawal Create(
        CollectionPointId pointId,
        FamilyId familyId,
        BeneficiaryId beneficiaryId,
        DateTime occurredAtUtc,
        string? notes) =>
        new(WithdrawalId.New(), pointId, familyId, beneficiaryId, occurredAtUtc,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
}