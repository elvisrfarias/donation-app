using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Representação pública de um ponto de coleta (endereço + contato).</summary>
public sealed record CollectionPointResponse(
    Guid Id,
    string Name,
    string Type,
    AddressResponse Address,
    ContactResponse Contact,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record AddressResponse(
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State,
    string ZipCode);

public sealed record ContactResponse(string ResponsibleName, string Email, string Phone);

public static class CollectionPointMappings
{
    public static CollectionPointResponse ToResponse(this CollectionPoint point) =>
        new(
            point.Id.Value,
            point.Name,
            point.Type.ToString(),
            new AddressResponse(
                point.Address.Street,
                point.Address.Number,
                point.Address.Complement,
                point.Address.District,
                point.Address.City,
                point.Address.State,
                point.Address.FormattedZipCode),
            new ContactResponse(
                point.Contact.ResponsibleName,
                point.Contact.Email.Value,
                point.Contact.Phone.Formatted),
            point.IsActive,
            point.CreatedAtUtc);
}