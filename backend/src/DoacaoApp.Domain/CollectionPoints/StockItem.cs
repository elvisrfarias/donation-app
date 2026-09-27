using DoacaoApp.Domain.Common;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Item de estoque granular — Entity DENTRO do agregado <see cref="CollectionPoint"/>. O catálogo
/// de itens é próprio de cada ponto (não é compartilhado entre pontos do município). Só o próprio
/// ponto cria, atualiza e remove seus itens.
/// </summary>
public sealed class StockItem : Entity<StockItemId>
{
    private StockItem()
    {
        Name = string.Empty;
        Unit = string.Empty;
    }

    private StockItem(
        StockItemId id,
        CollectionPointId pointId,
        string name,
        string unit,
        ItemCategory category,
        int quantity,
        int goal,
        int alertThreshold,
        bool isTracked) : base(id)
    {
        PointId = pointId;
        Name = name;
        Unit = unit;
        Category = category;
        Quantity = quantity;
        Goal = goal;
        AlertThreshold = alertThreshold;
        IsTracked = isTracked;
    }

    public CollectionPointId PointId { get; private set; }

    /// <summary>Nome do item (ex.: "Arroz 1kg"), único por ponto (sem diferenciar maiúsculas/minúsculas).</summary>
    public string Name { get; private set; }

    /// <summary>Unidade de medida em texto livre e curto (ex.: "kg", "un", "L", "pacote").</summary>
    public string Unit { get; private set; }

    /// <summary>Categoria do item, reaproveitada apenas para agrupamento/relatório.</summary>
    public ItemCategory Category { get; private set; }

    public int Quantity { get; private set; }
    public int Goal { get; private set; }
    public int AlertThreshold { get; private set; }
    public bool IsTracked { get; private set; }

    internal static StockItem Create(
        CollectionPointId pointId,
        string name,
        string unit,
        ItemCategory category,
        int quantity,
        int goal,
        int alertThreshold,
        bool isTracked) =>
        new(StockItemId.New(), pointId, name.Trim(), unit.Trim(), category, quantity, goal, alertThreshold, isTracked);

    internal void UpdateDetails(
        string name, string unit, ItemCategory category, int goal, int alertThreshold, bool isTracked)
    {
        Name = name.Trim();
        Unit = unit.Trim();
        Category = category;
        Goal = goal;
        AlertThreshold = alertThreshold;
        IsTracked = isTracked;
    }

    internal void ReplaceQuantity(int quantity) => Quantity = quantity;

    internal void Decrease(int amount) => Quantity -= amount;

    internal void Increase(int amount) => Quantity += amount;
}
