using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Item retirado numa visita, já com nome/unidade/categoria resolvidos.</summary>
public sealed record WithdrawalItemResponse(
    Guid StockItemId,
    string ItemName,
    string Unit,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    int Quantity);

/// <summary>Representação de uma retirada registrada.</summary>
public sealed record WithdrawalResponse(
    Guid Id,
    Guid PointId,
    Guid BeneficiaryId,
    IReadOnlyList<WithdrawalItemResponse> Items,
    Guid? BasketTemplateId,
    int? BasketQuantity,
    DateTime OccurredAtUtc,
    string? Notes);

public static class WithdrawalMappings
{
    private const string RemovedItemName = "Item removido";
    private const string RemovedItemUnit = "-";
    private const string RemovedCategoryName = "Categoria removida";
    private const string RemovedCategoryIcon = "help-circle";

    /// <summary>
    /// Mapeia a retirada para o contrato de resposta, resolvendo cada item a partir de
    /// <paramref name="stockItems"/> (nome/unidade/categoria) e <paramref name="categories"/>
    /// (nome/ícone da categoria do item). Quem carrega uma única retirada dentro do próprio ponto
    /// (registro/edição/painel) passa <c>point.StockItems</c>/<c>point.Categories</c>, sem I/O extra;
    /// quem lista o histórico municipal de um beneficiário — que abrange vários pontos — passa a
    /// coleção combinada dos itens e categorias de todos os pontos envolvidos. O item de estoque é
    /// protegido contra remoção enquanto referenciado por alguma retirada (FK <c>Restrict</c> +
    /// <see cref="Domain.Errors.DomainErrors.CollectionPoint.StockItemInUseByWithdrawal"/>), então o
    /// fallback abaixo é só defensivo — não deveria ser alcançável em produção.
    /// </summary>
    public static WithdrawalResponse ToResponse(
        this Withdrawal withdrawal,
        IReadOnlyCollection<StockItem> stockItems,
        IReadOnlyCollection<Category> categories)
    {
        return new(
            withdrawal.Id.Value,
            withdrawal.PointId.Value,
            withdrawal.BeneficiaryId.Value,
            [.. withdrawal.Items.Select(item => item.ToResponse(stockItems, categories))],
            withdrawal.BasketTemplateId?.Value,
            withdrawal.BasketQuantity,
            withdrawal.OccurredAtUtc,
            withdrawal.Notes);
    }

    private static WithdrawalItemResponse ToResponse(
        this WithdrawalItem item,
        IReadOnlyCollection<StockItem> stockItems,
        IReadOnlyCollection<Category> categories)
    {
        var stockItem = stockItems.FirstOrDefault(candidate => candidate.Id == item.StockItemId);
        if (stockItem is null)
        {
            return new WithdrawalItemResponse(
                item.StockItemId.Value,
                RemovedItemName,
                RemovedItemUnit,
                Guid.Empty,
                RemovedCategoryName,
                RemovedCategoryIcon,
                item.Quantity);
        }

        var category = categories.FirstOrDefault(candidate => candidate.Id == stockItem.CategoryId);
        return new WithdrawalItemResponse(
            item.StockItemId.Value,
            stockItem.Name,
            stockItem.Unit,
            stockItem.CategoryId.Value,
            category?.Name ?? RemovedCategoryName,
            category?.Icon ?? RemovedCategoryIcon,
            item.Quantity);
    }
}
