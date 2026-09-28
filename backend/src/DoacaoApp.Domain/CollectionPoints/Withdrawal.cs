using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Item livre de uma retirada: quantidade de um <see cref="StockItem"/> do mesmo ponto entregue nesta visita.</summary>
public sealed record WithdrawalItem(StockItemId StockItemId, int Quantity);

/// <summary>
/// Retirada — Entity DENTRO do agregado <see cref="CollectionPoint"/>. Referencia o
/// beneficiário por Id (é outro agregado). O conjunto de retiradas de um beneficiário,
/// somado entre todos os pontos, forma o histórico municipal compartilhado.
/// </summary>
public sealed class Withdrawal : Entity<WithdrawalId>
{
    private readonly List<WithdrawalItem> _items = [];

    private Withdrawal()
    {
    }

    private Withdrawal(
        WithdrawalId id,
        CollectionPointId pointId,
        BeneficiaryId beneficiaryId,
        IEnumerable<WithdrawalItem> items,
        DateTime occurredAtUtc,
        string? notes,
        BasketTemplateId? basketTemplateId,
        int? basketQuantity) : base(id)
    {
        PointId = pointId;
        BeneficiaryId = beneficiaryId;
        _items.AddRange(items);
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
        BasketTemplateId = basketTemplateId;
        BasketQuantity = basketQuantity;
    }

    public CollectionPointId PointId { get; private set; }
    public BeneficiaryId BeneficiaryId { get; private set; }
    /// <summary>Itens livres retirados na visita (roupas, calçados…), com quantidade — dão baixa no estoque do ponto.</summary>
    public IReadOnlyList<WithdrawalItem> Items => _items.AsReadOnly();

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
        IEnumerable<WithdrawalItem> items,
        DateTime occurredAtUtc,
        string? notes,
        (BasketTemplateId Id, int Quantity)? basket = null) =>
        new(
            WithdrawalId.New(), pointId, beneficiaryId, items, occurredAtUtc, NormalizeNotes(notes),
            basket?.Id, basket?.Quantity);

    internal void UpdateDetails(
        IEnumerable<WithdrawalItem> items, string? notes, (BasketTemplateId Id, int Quantity)? basket = null)
    {
        _items.Clear();
        _items.AddRange(items);
        Notes = NormalizeNotes(notes);
        BasketTemplateId = basket?.Id;
        BasketQuantity = basket?.Quantity;
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
