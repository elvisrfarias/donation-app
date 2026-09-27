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
        string? notes,
        BasketTemplateId? basketTemplateId,
        int? basketQuantity) : base(id)
    {
        PointId = pointId;
        BeneficiaryId = beneficiaryId;
        _categories.AddRange(categories);
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
        BasketTemplateId = basketTemplateId;
        BasketQuantity = basketQuantity;
    }

    public CollectionPointId PointId { get; private set; }
    public BeneficiaryId BeneficiaryId { get; private set; }
    /// <summary>Itens retirados na visita (cestas básicas, roupas, calçados…), sem repetição.</summary>
    public IReadOnlyList<ItemCategory> Categories => _categories.AsReadOnly();

    /// <summary>Receita de cesta básica retirada nesta visita, se houver — dá baixa automática no estoque do ponto.</summary>
    public BasketTemplateId? BasketTemplateId { get; private set; }

    /// <summary>Quantidade de cestas retiradas segundo <see cref="BasketTemplateId"/>.</summary>
    public int? BasketQuantity { get; private set; }

    /// <summary>Indica se a retirada é sujeita ao limite por ciclo — hoje, sempre que inclui uma cesta básica.</summary>
    public bool CountsTowardsLimit => BasketTemplateId is not null;

    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }

    internal static Withdrawal Create(
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        IEnumerable<ItemCategory> categories,
        DateTime occurredAtUtc,
        string? notes,
        (BasketTemplateId Id, int Quantity)? basket = null) =>
        new(
            WithdrawalId.New(), pointId, beneficiaryId, categories, occurredAtUtc, NormalizeNotes(notes),
            basket?.Id, basket?.Quantity);

    internal void UpdateDetails(
        IEnumerable<ItemCategory> categories, string? notes, (BasketTemplateId Id, int Quantity)? basket = null)
    {
        _categories.Clear();
        _categories.AddRange(categories);
        Notes = NormalizeNotes(notes);
        BasketTemplateId = basket?.Id;
        BasketQuantity = basket?.Quantity;
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
