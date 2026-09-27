using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Item de uma receita de cesta básica: quantidade necessária de um <see cref="StockItem"/> do mesmo ponto.</summary>
public sealed record BasketTemplateItem(StockItemId StockItemId, int Quantity);

/// <summary>
/// Receita de cesta básica — Entity DENTRO do agregado <see cref="CollectionPoint"/>. Descreve
/// quanto de cada item do estoque do ponto compõe uma cesta completa. Um ponto pode ter várias
/// receitas configuráveis (não só "a" cesta básica).
/// </summary>
public sealed class BasketTemplate : Entity<BasketTemplateId>
{
    private readonly List<BasketTemplateItem> _items = [];

    private BasketTemplate()
    {
        Name = string.Empty;
    }

    private BasketTemplate(
        BasketTemplateId id,
        CollectionPointId pointId,
        string name,
        IEnumerable<BasketTemplateItem> items,
        bool isActive) : base(id)
    {
        PointId = pointId;
        Name = name;
        _items.AddRange(items);
        IsActive = isActive;
    }

    public CollectionPointId PointId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyList<BasketTemplateItem> Items => _items.AsReadOnly();

    internal static BasketTemplate Create(
        CollectionPointId pointId, string name, IEnumerable<BasketTemplateItem> items) =>
        new(BasketTemplateId.New(), pointId, name.Trim(), items, isActive: true);

    internal void UpdateDetails(string name, IEnumerable<BasketTemplateItem> items, bool isActive)
    {
        Name = name.Trim();
        _items.Clear();
        _items.AddRange(items);
        IsActive = isActive;
    }
}
