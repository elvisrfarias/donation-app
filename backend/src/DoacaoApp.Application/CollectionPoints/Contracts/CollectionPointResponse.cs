using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>
/// Representação pública de um ponto de coleta (endereço + contato + avaliações).
/// <see cref="Categories"/> vem sempre preenchido (é o catálogo que o ponto declara aceitar,
/// mostrado no mapa público); já <see cref="StockItems"/> e <see cref="BasketTemplates"/> só vêm
/// preenchidos na visão de gestão (<c>includeManagementDetails: true</c>) — a listagem pública
/// devolve as duas listas vazias.
/// </summary>
public sealed record CollectionPointResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string? OpeningHours,
    IReadOnlyList<string> Needs,
    IReadOnlyList<CategoryResponse> Categories,
    IReadOnlyList<StockItemResponse> StockItems,
    IReadOnlyList<BasketTemplateResponse> BasketTemplates,
    WithdrawalRuleResponse WithdrawalRule,
    AddressResponse Address,
    ContactResponse Contact,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? ImageUrl,
    double AverageRating,
    int ReviewCount,
    int BeneficiariesServed);

/// <summary>Categoria do catálogo do ponto (agrupa estoque, checklist de retirada e itens aceitos no mapa público).</summary>
public sealed record CategoryResponse(Guid Id, string Name, string Icon);

/// <summary>Classificação do estoque de um item: "out" (em falta), "low" (baixo), "goal" (meta atingida) ou "ok".</summary>
public sealed record StockItemResponse(
    Guid Id,
    string Name,
    string Unit,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    int Quantity,
    int Goal,
    int AlertThreshold,
    bool IsTracked,
    string Status);

public sealed record BasketTemplateResponse(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<BasketTemplateItemResponse> Items,
    BasketFeasibilityResponse Feasibility);

public sealed record BasketTemplateItemResponse(Guid StockItemId, string ItemName, int Quantity);

public sealed record BasketFeasibilityResponse(int CompleteBaskets, IReadOnlyList<BasketItemGapResponse> Missing);

public sealed record BasketItemGapResponse(
    Guid ItemId, string ItemName, int RequiredPerBasket, int CurrentQuantity, int NeededForNextBasket);

public sealed record WithdrawalRuleResponse(string Type, int MaxWithdrawalsPerCycle);

public sealed record AddressResponse(
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State,
    string ZipCode,
    double? Latitude,
    double? Longitude);

public sealed record ContactResponse(string ResponsibleName, string Email, string Phone);

public static class CollectionPointMappings
{
    private const string StockStatusOut = "out";
    private const string StockStatusLow = "low";
    private const string StockStatusGoalReached = "goal";
    private const string StockStatusOk = "ok";

    public static CollectionPointResponse ToResponse(
        this CollectionPoint point,
        string? imageUrl = null,
        double averageRating = 0,
        int reviewCount = 0,
        int beneficiariesServed = 0,
        bool includeManagementDetails = false) =>
        new(
            point.Id.Value,
            point.Name,
            point.Type.ToString(),
            point.Description,
            point.OpeningHours,
            [.. point.Needs],
            [.. point.Categories.Select(ToResponse)],
            includeManagementDetails ? point.StockItems.Select(item => item.ToResponse(point)).ToList() : [],
            includeManagementDetails ? point.BasketTemplates.Select(template => template.ToResponse(point)).ToList() : [],
            new WithdrawalRuleResponse(point.WithdrawalRule.Type.ToString(), point.WithdrawalRule.MaxWithdrawalsPerCycle),
            new AddressResponse(
                point.Address.Street,
                point.Address.Number,
                point.Address.Complement,
                point.Address.District,
                point.Address.City,
                point.Address.State,
                point.Address.FormattedZipCode,
                point.Address.Latitude,
                point.Address.Longitude),
            new ContactResponse(
                point.Contact.ResponsibleName,
                point.Contact.Email.Value,
                point.Contact.Phone.Formatted),
            point.IsActive,
            point.CreatedAtUtc,
            imageUrl,
            averageRating,
            reviewCount,
            beneficiariesServed);

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id.Value, category.Name, category.Icon);

    private static StockItemResponse ToResponse(this StockItem item, CollectionPoint point)
    {
        var category = point.Categories.Single(candidate => candidate.Id == item.CategoryId);
        return new(
            item.Id.Value,
            item.Name,
            item.Unit,
            item.CategoryId.Value,
            category.Name,
            category.Icon,
            item.Quantity,
            item.Goal,
            item.AlertThreshold,
            item.IsTracked,
            StatusOf(item.Quantity, item.Goal, item.AlertThreshold));
    }

    private static BasketTemplateResponse ToResponse(this BasketTemplate template, CollectionPoint point)
    {
        var feasibility = point.CalculateBasketFeasibility(template.Id);

        return new BasketTemplateResponse(
            template.Id.Value,
            template.Name,
            template.IsActive,
            [.. template.Items.Select(item => item.ToResponse(point))],
            feasibility.ToResponse());
    }

    private static BasketTemplateItemResponse ToResponse(this BasketTemplateItem item, CollectionPoint point)
    {
        var stockItem = point.StockItems.Single(candidate => candidate.Id == item.StockItemId);
        return new BasketTemplateItemResponse(item.StockItemId.Value, stockItem.Name, item.Quantity);
    }

    private static BasketFeasibilityResponse ToResponse(this BasketFeasibility feasibility) =>
        new(
            feasibility.CompleteBaskets,
            [.. feasibility.Missing.Select(ToResponse)]);

    private static BasketItemGapResponse ToResponse(BasketItemGap gap) =>
        new(gap.ItemId.Value, gap.ItemName, gap.RequiredPerBasket, gap.CurrentQuantity, gap.NeededForNextBasket);

    /// <summary>
    /// Mesma classificação que hoje o front deriva de goal/alerta (ver <c>stockStatus</c> em
    /// <c>stockDraft.ts</c>): sem estoque, baixo (abaixo ou igual ao aviso), meta atingida, ou ok.
    /// </summary>
    private static string StatusOf(int quantity, int goal, int alertThreshold)
    {
        if (quantity <= 0)
        {
            return StockStatusOut;
        }

        if (quantity <= alertThreshold)
        {
            return StockStatusLow;
        }

        return goal > 0 && quantity >= goal ? StockStatusGoalReached : StockStatusOk;
    }
}
