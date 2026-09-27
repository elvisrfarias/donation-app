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
    private readonly List<ItemCategory> _categories = [];

    private Withdrawal()
    {
    }

    private Withdrawal(
        WithdrawalId id,
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        IEnumerable<ItemCategory> categories,
        DateTime occurredAtUtc,
        string? notes) : base(id)
    {
        PointId = pointId;
        BeneficiaryId = beneficiaryId;
        _categories.AddRange(categories);
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
    }

    public CollectionPointId PointId { get; private set; }
    public BeneficiaryId BeneficiaryId { get; private set; }
    /// <summary>Itens retirados na visita (cestas básicas, roupas, calçados…), sem repetição.</summary>
    public IReadOnlyList<ItemCategory> Categories => _categories.AsReadOnly();

    /// <summary>Indica se a retirada inclui algum item sujeito ao limite por ciclo.</summary>
    public bool CountsTowardsLimit => _categories.Any(category => category.CountsTowardsWithdrawalLimit());

    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }

    internal static Withdrawal Create(
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        IEnumerable<ItemCategory> categories,
        DateTime occurredAtUtc,
        string? notes) =>
        new(WithdrawalId.New(), pointId, beneficiaryId, categories, occurredAtUtc, NormalizeNotes(notes));

    internal void UpdateDetails(IEnumerable<ItemCategory> categories, string? notes)
    {
        _categories.Clear();
        _categories.AddRange(categories);
        Notes = NormalizeNotes(notes);
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
