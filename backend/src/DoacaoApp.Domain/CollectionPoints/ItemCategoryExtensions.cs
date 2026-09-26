namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>Regras que dependem da categoria do item retirado.</summary>
public static class ItemCategoryExtensions
{
    /// <summary>Única categoria sujeita ao limite por ciclo do ponto (as demais são livres, mas registradas).</summary>
    public const ItemCategory WithdrawalLimitedCategory = ItemCategory.Food;

    public static bool CountsTowardsWithdrawalLimit(this ItemCategory category) =>
        category == WithdrawalLimitedCategory;
}
