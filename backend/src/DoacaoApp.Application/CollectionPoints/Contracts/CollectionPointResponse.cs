using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Representação pública de um ponto de coleta (endereço + contato + avaliações).</summary>
public sealed record CollectionPointResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string? OpeningHours,
    IReadOnlyList<string> Needs,
    IReadOnlyList<StockItemResponse> Stock,
    IReadOnlyList<StockSettingResponse> StockSettings,
    WithdrawalRuleResponse WithdrawalRule,
    AddressResponse Address,
    ContactResponse Contact,
    IReadOnlyList<string> AcceptedItems,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? ImageUrl,
    double AverageRating,
    int ReviewCount,
    int BeneficiariesServed);

public sealed record StockItemResponse(string Category, int Quantity);

public sealed record WithdrawalRuleResponse(string Type, int MaxWithdrawalsPerCycle);

public sealed record StockSettingResponse(string Category, int Goal, int AlertThreshold, bool IsTracked);

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
    public static CollectionPointResponse ToResponse(
        this CollectionPoint point,
        string? imageUrl = null,
        double averageRating = 0,
        int reviewCount = 0,
        int beneficiariesServed = 0,
        bool includeStockSettings = false) =>
        new(
            point.Id.Value,
            point.Name,
            point.Type.ToString(),
            point.Description,
            point.OpeningHours,
            [.. point.Needs],
            [.. point.Stock.OrderBy(kv => (int)kv.Key).Select(kv => new StockItemResponse(kv.Key.ToString(), kv.Value))],
            includeStockSettings
                ? [.. point.StockSettings
                    .OrderBy(kv => (int)kv.Key)
                    .Select(kv => new StockSettingResponse(
                        kv.Key.ToString(), kv.Value.Goal, kv.Value.AlertThreshold, kv.Value.IsTracked))]
                : [],
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
            [.. point.AcceptedItems.Select(static i => i.ToString())],
            point.IsActive,
            point.CreatedAtUtc,
            imageUrl,
            averageRating,
            reviewCount,
            beneficiariesServed);
}