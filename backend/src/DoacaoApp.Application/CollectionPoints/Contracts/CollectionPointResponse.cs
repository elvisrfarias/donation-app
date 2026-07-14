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
    AddressResponse Address,
    ContactResponse Contact,
    IReadOnlyList<string> AcceptedItems,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? ImageUrl,
    double AverageRating,
    int ReviewCount,
    int FamiliesServed);

public sealed record StockItemResponse(string Category, int Quantity);

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
        int familiesServed = 0) =>
        new(
            point.Id.Value,
            point.Name,
            point.Type.ToString(),
            point.Description,
            point.OpeningHours,
            [.. point.Needs],
            [.. point.Stock.OrderBy(kv => (int)kv.Key).Select(kv => new StockItemResponse(kv.Key.ToString(), kv.Value))],
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
            familiesServed);
}