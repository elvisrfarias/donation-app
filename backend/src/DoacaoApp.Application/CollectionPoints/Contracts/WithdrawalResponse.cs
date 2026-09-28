using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Item livre retirado numa visita (roupas, calçados…), já com nome/ícone resolvidos.</summary>
public sealed record WithdrawalCategoryResponse(Guid CategoryId, string CategoryName, string CategoryIcon);

/// <summary>Representação de uma retirada registrada.</summary>
public sealed record WithdrawalResponse(
    Guid Id,
    Guid PointId,
    Guid BeneficiaryId,
    IReadOnlyList<WithdrawalCategoryResponse> Categories,
    Guid? BasketTemplateId,
    int? BasketQuantity,
    DateTime OccurredAtUtc,
    string? Notes);

public static class WithdrawalMappings
{
    private const string RemovedCategoryName = "Categoria removida";
    private const string RemovedCategoryIcon = "help-circle";

    /// <summary>
    /// Mapeia a retirada para o contrato de resposta, resolvendo nome/ícone de cada categoria a
    /// partir de <paramref name="categories"/>. Quem carrega uma única retirada dentro do próprio
    /// ponto (registro/edição) passa <c>point.Categories</c>, sem I/O extra; quem lista o histórico
    /// municipal de um beneficiário — que abrange vários pontos — passa uma coleção combinada das
    /// categorias de todos os pontos envolvidos.
    /// </summary>
    public static WithdrawalResponse ToResponse(this Withdrawal withdrawal, IReadOnlyCollection<Category> categories)
    {
        var categoriesById = categories.ToDictionary(category => category.Id);

        return new(
            withdrawal.Id.Value,
            withdrawal.PointId.Value,
            withdrawal.BeneficiaryId.Value,
            [.. withdrawal.CategoryIds.Select(categoryId => categoryId.ToResponse(categoriesById))],
            withdrawal.BasketTemplateId?.Value,
            withdrawal.BasketQuantity,
            withdrawal.OccurredAtUtc,
            withdrawal.Notes);
    }

    private static WithdrawalCategoryResponse ToResponse(
        this CategoryId categoryId, IReadOnlyDictionary<CategoryId, Category> categoriesById)
    {
        var category = categoriesById.GetValueOrDefault(categoryId);
        return new WithdrawalCategoryResponse(
            categoryId.Value,
            category?.Name ?? RemovedCategoryName,
            category?.Icon ?? RemovedCategoryIcon);
    }
}
